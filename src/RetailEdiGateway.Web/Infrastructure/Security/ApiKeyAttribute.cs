using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace RetailEdiGateway.Web.Infrastructure.Security
{
 /// <summary>
 /// Action Filter attribute to enforce API Key authentication on controller actions.
 /// Expects 'X-API-KEY' header to be present and match the configured value.
 /// </summary>
 [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Method)]
 public class ApiKeyAttribute : Attribute, IAsyncActionFilter
 {
 private const string ApiKeyHeaderName = "X-API-KEY";

 /// <summary>
 /// Validates the presence and value of the API Key header before executing the action.
 /// </summary>
 public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
 {
 if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
 {
 context.Result = new ContentResult()
 {
 StatusCode = 401,
 Content = "API Key was not provided."
 };
 return;
 }

 var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
 var apiKey = configuration.GetValue<string>("Authentication:ApiKey");

 if (string.IsNullOrEmpty(apiKey) || !apiKey.Equals(extractedApiKey))
 {
 context.Result = new ContentResult()
 {
 StatusCode = 401,
 Content = "Unauthorized client."
 };
 return;
 }

 await next();
 }
 }
}
