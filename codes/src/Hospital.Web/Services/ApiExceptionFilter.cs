using System.Net;
using Hospital.Client;
using Hospital.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Hospital.Web.Services;

public sealed class ApiExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not HospitalApiException error) return;
        if (error.Status == HttpStatusCode.Unauthorized) context.Result = new ChallengeResult();
        else
        {
            var title = error.Status == HttpStatusCode.Forbidden ? "Bạn không có quyền truy cập" : "Chưa thể tải thông tin";
            context.Result = new ViewResult { ViewName = "ApiError", StatusCode = (int)error.Status,
                ViewData = new ViewDataDictionary(new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(), context.ModelState)
                { Model = new UiMessage(title, error.Message, "error", error.TraceId) } };
        }
        context.ExceptionHandled = true;
    }
}
