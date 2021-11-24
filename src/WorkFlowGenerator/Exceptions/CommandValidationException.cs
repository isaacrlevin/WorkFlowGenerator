using System;
using System.Collections.Generic;
using System.Text;

namespace WorkFlowGenerator.Exceptions;

public class CommandValidationException : Exception
{
    public CommandValidationException(string message)
        : base(message)
    {
    }

    public CommandValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
