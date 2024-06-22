using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Coree.NETASP.UnderConstruction
{
    public class DummyFileProvider : IFileProvider
    {
        private readonly string root;

        public DummyFileProvider(string root)
        {
            this.root = root;
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            throw new NotImplementedException();
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            subpath = subpath.TrimStart(new char[] { '/', '\\' });
            FileInfo fileInfo = new FileInfo(System.IO.Path.Combine(root, subpath));
            if (fileInfo.Exists)
            {
                return new PhysicalFileInfo(fileInfo);
            }
            else
            {
                return new DummyFileInfo("example.txt");
            }

            // Return a dummy IFileInfo that pretends there is always a specific file present
        }

        public IChangeToken Watch(string filter)
        {
            throw new NotImplementedException();
        }

        private class DummyFileInfo : IFileInfo
        {
            private readonly string _content = "This is a fixed content for all files.";
            private readonly string _name;

            public DummyFileInfo(string name)
            {
                _name = name;
            }

            public bool Exists => true;

            public long Length => _content.Length;

            public string PhysicalPath => null; // No physical path

            public string Name => _name;

            public DateTimeOffset LastModified => DateTimeOffset.UtcNow; // You can adjust this as needed

            public bool IsDirectory => false;

            public Stream CreateReadStream()
            {
                // Return a stream that contains the fixed content
                return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(_content));
            }
        }
    }
}
