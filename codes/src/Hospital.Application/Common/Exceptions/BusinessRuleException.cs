namespace Hospital.Application.Common.Exceptions;

public class BusinessRuleException : Exception
{
    public string ErrorCode { get; }

    public BusinessRuleException(string message, string errorCode = "BUSINESS_RULE_VIOLATION") : base(message)
    {
        ErrorCode = errorCode;
    }
}

public class NotFoundException : Exception
{
    public NotFoundException(string name, object key) : base($"Thực thể \"{name}\" ({key}) không tồn tại trong hệ thống.")
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }
}

public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException(string message = "Bạn không có quyền thực hiện thao tác này.") : base(message)
    {
    }
}

public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message = "Dữ liệu đã bị thay đổi bởi người dùng khác. Vui lòng tải lại trang và thử lại.") : base(message)
    {
    }
}
