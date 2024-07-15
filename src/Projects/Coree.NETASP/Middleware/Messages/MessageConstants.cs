namespace Coree.NETASP.Middleware.Messages
{
    /// <summary>
    /// A static class containing grouped string constants.
    /// </summary>
    public static class MessageConstants
    {
        /// <summary>
        /// Messages related to user actions.
        /// </summary>
        public static class UserActions
        {
            public const string LoginMessage = "User logged in successfully.";
            public const string LogoutMessage = "User logged out successfully.";
        }

        public static class ErrorMessages
        {
            public const string UnknownErrorMessage = "An unknown error has occurred.";
            public const string NetworkErrorMessage = "A network error has occurred.";
        }
    }

}
