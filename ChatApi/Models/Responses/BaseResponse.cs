using System;
using System.Collections.Generic;

namespace ChatApi.Models
{
    public class BaseResponse<T>
    {
        public T Data { get; set; }
        public string Error { get; set; }
        public Dictionary<string, string> Validations { get; set; }
    }
}