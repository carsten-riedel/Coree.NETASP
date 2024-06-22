using Microsoft.Extensions.Options;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Coree.NETASP.UnderConstruction
{

    public class ServicesDumper
    {
        private readonly IServiceCollection _services;

        public ServicesDumper(IServiceCollection services)
        {
            _services = services;
        }

        public void DumpAllServices()
        {
            _services.BuildServiceProvider();


            foreach (var service in _services)
            {
                if (service.ServiceType.IsGenericType &&
                    (service.ServiceType.GetGenericTypeDefinition() == typeof(IConfigureOptions<>) ||
                    service.ServiceType.GetGenericTypeDefinition() == typeof(IPostConfigureOptions<>) ||
                    service.ServiceType.GetGenericTypeDefinition() == typeof(IOptions<>))
                    )
                {


                    Console.WriteLine($"Service: {FormatTypeName(service.ServiceType)}, Implementation: {(service.ImplementationType != null ? FormatTypeName(service.ImplementationType) : "Factory/Instance")}");

                    AnalyzeOptionsService(service);
                }
            }
        }

        private void AnalyzeOptionsService(ServiceDescriptor service)
        {
            Console.WriteLine($"  -> Analyzing Options Service: {FormatTypeName(service.ServiceType)}");

            try
            {
                object instance = service.ImplementationInstance ?? service.ImplementationFactory?.Invoke(_services.BuildServiceProvider());
                if (instance != null)
                {
                    DumpOptionsInstance(instance);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"  -> Error during service analysis: {ex.Message}");
            }
        }

        private void DumpOptionsInstance(object instance)
        {
            var optionsType = instance.GetType();
            var actionProperty = optionsType.GetProperty("Action", BindingFlags.Instance | BindingFlags.Public);

            if (actionProperty != null)
            {
                var delegateInstance = actionProperty.GetValue(instance) as Delegate;
                if (delegateInstance != null)
                {
                    try
                    {
                        // Determine the parameter type by inspecting the delegate directly

                        var fooype = delegateInstance.Method.GetParameters();
                        Type parameterType = delegateInstance.Method.GetParameters()[0].ParameterType;

                        // Create an instance of the options type that the Action<T> expects
                        object optionsInstance = Activator.CreateInstance(parameterType);

                        // Directly invoke the delegate with the newly created options instance
                        delegateInstance.DynamicInvoke(optionsInstance);

                        ObjectDumper3.Dump(optionsInstance, 0, 10);

                        //Console.WriteLine($"  -> Configured Options: {parameterType.Name}");
                        //foreach (var property in optionsInstance.GetType().GetProperties())
                        //{
                        //    object value = property.GetValue(optionsInstance);
                        //    Console.WriteLine($"    -> {property.Name}: {value}");
                        //}
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  -> Error invoking options configuration: {ex.InnerException?.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine("  -> No actionable configuration found.");
            }
        }


        private string FormatTypeName(Type type)
        {
            if (!type.IsGenericType)
                return type.FullName;

            var genericTypeName = type.GetGenericTypeDefinition().Name;
            genericTypeName = genericTypeName.Substring(0, genericTypeName.IndexOf('`'));
            var genericArgs = string.Join(", ", type.GetGenericArguments().Select(FormatTypeName));
            return $"{genericTypeName}<{genericArgs}>";
        }
    }
    public class OptionsDumper
    {
        private readonly IServiceProvider _serviceProvider;

        public OptionsDumper(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void DumpAllOptions()
        {
            // Getting the generic type definition for IOptions<>
            var optionsType = typeof(IOptions<>);

            // Fetch all services that are IOptions<T>
            var optionsServices = _serviceProvider.GetServices(optionsType)
                                                  .Select(service => service.GetType().GenericTypeArguments[0])
                                                  .Distinct();

            foreach (var service in optionsServices)
            {
                Console.WriteLine($"Options: {service.Name}");

                // Make the generic type IOptions<T>
                var constructedOptionsType = optionsType.MakeGenericType(service);
                var optionsService = _serviceProvider.GetService(constructedOptionsType);

                if (optionsService != null)
                {
                    // Reflect to get the 'Value' property
                    var valueProperty = constructedOptionsType.GetProperty("Value");
                    var optionsValue = valueProperty.GetValue(optionsService);
                    DumpProperties(optionsValue, "  ");
                }
                else
                {
                    Console.WriteLine("  Options service not found.");
                }
            }
        }

        private void DumpProperties(object obj, string indent)
        {
            if (obj == null)
            {
                Console.WriteLine($"{indent}(null)");
                return;
            }

            var type = obj.GetType();
            var properties = type.GetProperties();
            if (properties.Length == 0)
            {
                // Handle simple object types
                Console.WriteLine($"{indent}{obj}");
            }
            else
            {
                foreach (var property in properties)
                {
                    var propValue = property.GetValue(obj);
                    Console.WriteLine($"{indent}{property.Name}: {propValue}");
                }
            }
        }
    }


    public static class ObjectDumper
    {
        /// <summary>
        /// Dump the details of an object to the console.
        /// </summary>
        /// <param name="obj">The object to dump.</param>
        /// <param name="depth">The current depth to control the recursion level.</param>
        /// <param name="maxDepth">The maximum depth to recurse through the object graph.</param>
        public static void Dump(object obj, int depth = 0, int maxDepth = 5)
        {
            if (depth > maxDepth)
            {
                Console.WriteLine(new string(' ', depth * 3) + "...");
                return;
            }

            if (obj == null)
            {
                Console.WriteLine("null");
                return;
            }

            Type type = obj.GetType();
            if (Type.GetTypeCode(type) != TypeCode.Object || type == typeof(string))
            {
                Console.WriteLine(new string(' ', depth * 3) + obj + " (" + type.Name + ")");
            }
            else if (obj is System.Collections.IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    Dump(item, depth + 1, maxDepth);
                }
            }
            else
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                Console.WriteLine(new string(' ', depth * 3) + type.Name + " {");

                foreach (FieldInfo field in fields)
                {
                    object fieldValue = field.GetValue(obj);
                    Console.Write(new string(' ', (depth + 1) * 3) + field.Name + ": ");
                    Dump(fieldValue, depth + 1, maxDepth);
                }

                foreach (PropertyInfo property in properties)
                {
                    object propertyValue = property.GetValue(obj);
                    Console.Write(new string(' ', (depth + 1) * 3) + property.Name + ": ");
                    Dump(propertyValue, depth + 1, maxDepth);
                }

                Console.WriteLine(new string(' ', depth * 3) + "}");
            }
        }
    }

    public static class ObjectDumper2
    {
        /// <summary>
        /// Dump the details of an object to the console.
        /// </summary>
        /// <param name="obj">The object to dump.</param>
        /// <param name="depth">The current depth to control the recursion level.</param>
        /// <param name="maxDepth">The maximum depth to recurse through the object graph.</param>
        /// <remarks>Handles circular references and differentiates collection types.</remarks>
        public static void Dump(object obj, int depth = 0, int maxDepth = 5)
        {
            HashSet<object> visited = new HashSet<object>(new ObjectReferenceEqualityComparer());
            DumpInternal(obj, depth, maxDepth, visited);
        }

        private static void DumpInternal(object obj, int depth, int maxDepth, HashSet<object> visited)
        {
            if (depth > maxDepth)
            {
                Console.WriteLine(new string(' ', depth * 3) + "...");
                return;
            }

            if (obj == null)
            {
                Console.WriteLine(new string(' ', depth * 3) + "null");
                return;
            }

            if (!visited.Add(obj))
            {
                Console.WriteLine(new string(' ', depth * 3) + "circular reference detected...");
                return;
            }

            Type type = obj.GetType();
            if (Type.GetTypeCode(type) != TypeCode.Object || type == typeof(string))
            {
                Console.WriteLine(new string(' ', depth * 3) + obj + " (" + type.Name + ")");
            }
            else if (obj is Array array)
            {
                Console.WriteLine(new string(' ', depth * 3) + type.GetElementType().Name + "[] {");
                int index = 0;
                foreach (var item in array)
                {
                    Console.Write(new string(' ', (depth + 1) * 3) + $"[{index++}]: ");
                    DumpInternal(item, depth + 1, maxDepth, visited);
                }
                Console.WriteLine(new string(' ', depth * 3) + "}");
            }
            else if (obj is System.Collections.IEnumerable enumerable)
            {
                Console.WriteLine(new string(' ', depth * 3) + type.Name + " [");
                foreach (var item in enumerable)
                {
                    DumpInternal(item, depth + 1, maxDepth, visited);
                }
                Console.WriteLine(new string(' ', depth * 3) + "]");
            }
            else
            {
                ReflectAndDump(obj, type, depth, maxDepth, visited);
            }
        }


        private static void ReflectAndDump(object obj, Type type, int depth, int maxDepth, HashSet<object> visited)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            Console.WriteLine(new string(' ', depth * 3) + type.Name + " {");

            foreach (FieldInfo field in fields)
            {
                object fieldValue = field.GetValue(obj);
                Console.Write(new string(' ', (depth + 1) * 3) + field.Name + ": ");
                DumpInternal(fieldValue, depth + 1, maxDepth, visited);
            }

            foreach (PropertyInfo property in properties)
            {

                object propertyValue = property.GetValue(obj, null);
                Console.Write(new string(' ', (depth + 1) * 3) + property.Name + ": ");
                DumpInternal(propertyValue, depth + 1, maxDepth, visited);
            }

            Console.WriteLine(new string(' ', depth * 3) + "}");
        }
    }

    /// <summary>
    /// Equality comparer for object references.
    /// </summary>
    internal class ObjectReferenceEqualityComparer : EqualityComparer<object>
    {
        public override bool Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }

        public override int GetHashCode(object obj)
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }

    public static class ObjectDumper3
    {
        /// <summary>
        /// Dump the details of an object to the console.
        /// </summary>
        /// <param name="obj">The object to dump.</param>
        /// <param name="depth">The current depth to control the recursion level.</param>
        /// <param name="maxDepth">The maximum depth to recurse through the object graph.</param>
        /// <remarks>Differentiates between property types and fields.</remarks>
        public static void Dump(object obj, int depth = 0, int maxDepth = 5)
        {
            DumpInternal(obj, depth, maxDepth);
        }

        private static void DumpInternal(object obj, int depth, int maxDepth)
        {
            if (depth > maxDepth)
            {
                Console.WriteLine(new string(' ', depth * 3) + "...");
                return;
            }

            if (obj == null)
            {
                Console.WriteLine(new string(' ', depth * 3) + "null");
                return;
            }

            Type type = obj.GetType();
            if (Type.GetTypeCode(type) != TypeCode.Object || type == typeof(string))
            {
                Console.WriteLine(new string(' ', depth * 3) + obj + " (" + type.Name + ")");
            }
            else if (obj is Array array)
            {
                Console.WriteLine(new string(' ', depth * 3) + type.GetElementType().Name + "[] {");
                int index = 0;
                foreach (var item in array)
                {
                    Console.Write(new string(' ', (depth + 1) * 3) + $"[{index++}]: ");
                    DumpInternal(item, depth + 1, maxDepth);
                }
                Console.WriteLine(new string(' ', depth * 3) + "}");
            }
            else if (obj is System.Collections.IEnumerable enumerable)
            {
                Console.WriteLine(new string(' ', depth * 3) + type.Name + " [");
                foreach (var item in enumerable)
                {
                    DumpInternal(item, depth + 1, maxDepth);
                }
                Console.WriteLine(new string(' ', depth * 3) + "]");
            }
            else
            {
                ReflectAndDump(obj, type, depth, maxDepth, true);
            }
        }

        private static void ReflectAndDump(object obj, Type type, int depth, int maxDepth, bool publicOnly)
        {
            BindingFlags bindingFlags = publicOnly
                ? BindingFlags.Public | BindingFlags.Instance
                : BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            FieldInfo[] fields = type.GetFields(bindingFlags);
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance); // Already only public properties

            Console.WriteLine(new string(' ', depth * 3) + type.Name + " {");

            foreach (FieldInfo field in fields)
            {
                string fieldName = field.Name;
                object fieldValue = field.GetValue(obj);
                string label = "Field";

                // Check if the field is a compiler-generated backing field
                if (Regex.IsMatch(fieldName, @"\<[^>]+\>k__BackingField"))
                {
                    fieldName = fieldName.Replace("<", "").Replace(">k__BackingField", ""); // Clean up the field name
                    label = "BackingField";
                }

                Console.Write(new string(' ', (depth + 1) * 3) + fieldName + $" ({label}): ");
                DumpInternal(fieldValue, depth + 1, maxDepth);
            }

            foreach (PropertyInfo property in properties)
            {
                object propertyValue = property.GetValue(obj, null);
                Console.Write(new string(' ', (depth + 1) * 3) + property.Name + " (Property): ");
                DumpInternal(propertyValue, depth + 1, maxDepth);
            }

            Console.WriteLine(new string(' ', depth * 3) + "}");
        }

    }
}
