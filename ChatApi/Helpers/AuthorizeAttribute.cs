using System;
using ChatApi.EntityFramework.Models;
using ChatApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ChatApi.Helpers
{
    [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = (User) context.HttpContext.Items["User"];

            if (user == null)
            {
                context.Result = new JsonResult(
                    new BaseResponse<object>
                    {
                        Error = "unauthorized"
                    }                
                )
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
            }
        }
    }
}