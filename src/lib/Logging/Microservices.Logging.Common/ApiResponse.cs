using System;
using System.Collections.Generic;
using System.Text;

namespace Microservices.Logging.Common
{
    public interface IApiResponse
    {
        object ResponseResult();
    }
    public class ApiResponse<T> : IApiResponse
    {
        public string status;
        public T result;
        public object ResponseResult() => result;
    }
}
