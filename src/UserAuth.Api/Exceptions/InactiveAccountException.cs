namespace UserAuth.Api.Exceptions;

public class InactiveAccountException : Exception
{
    public InactiveAccountException(string message) : base(message)
    {
    }
}