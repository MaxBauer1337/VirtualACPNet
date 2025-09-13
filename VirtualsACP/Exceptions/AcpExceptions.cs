namespace VirtualsAcp.Exceptions;

public class AcpError : Exception
{
    public AcpError(string message) : base(message) { }
    public AcpError(string message, Exception innerException) : base(message, innerException) { }
}

public class AcpApiError : AcpError
{
    public AcpApiError(string message) : base(message) { }
    public AcpApiError(string message, Exception innerException) : base(message, innerException) { }
}

public class AcpContractError : AcpError
{
    public AcpContractError(string message) : base(message) { }
    public AcpContractError(string message, Exception innerException) : base(message, innerException) { }
}

public class TransactionFailedError : AcpContractError
{
    public TransactionFailedError(string message) : base(message) { }
    public TransactionFailedError(string message, Exception innerException) : base(message, innerException) { }
}
