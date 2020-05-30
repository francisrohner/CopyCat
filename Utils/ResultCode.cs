using System;
using System.Collections.Generic;
using System.Text;

namespace CopyCat.Utils
{
    public enum ResultCode
    {
        //Error range
        SOURCE_NOT_FOUND = -100,
        OS_MISMATCH,
        DIRECTORY_NOT_FOUND,
        FILE_NOT_FOUND,
        ARGUMENT_NULL_EXCEPTION,
        ARGUMENT_EXCEPTION,
        UNAUTHORIZED_EXCEPTION,
        HTTP_ERROR,
        FAILED = -1,

        
        UNDEFINED = 0,
        SUCCESS = 1,
        EXIT_REQUESTED = 2,
        NOT_IMPLEMENTED = 3,
    }
}
