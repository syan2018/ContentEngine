namespace ContentEngine.WebApp.Components.Pages.SchemaManagement.Create;

/// <summary>
/// 表单验证帮助类，提供统一的验证逻辑
/// </summary>
public static class FormValidationHelper
{
    /// <summary>
    /// 表单验证结果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 验证父组件表单并显示相应的错误消息
    /// </summary>
    /// <param name="validateParentForm">父组件表单验证回调</param>
    /// <param name="snackbar">消息提示服务</param>
    /// <param name="customErrorMessage">自定义错误消息，如果为空则使用默认消息</param>
    /// <returns>验证结果，包含是否有效和错误消息</returns>
    public static async Task<ValidationResult> ValidateParentFormAsync(
        Func<Task<bool>>? validateParentForm, 
        MudBlazor.ISnackbar snackbar,
        string? customErrorMessage = null)
    {
        if (validateParentForm != null)
        {
            bool isParentFormValid = await validateParentForm.Invoke();
            if (!isParentFormValid)
            {
                var errorMessage = customErrorMessage ?? "请先填写数据结构名称等基本信息。";
                snackbar.Add(errorMessage, MudBlazor.Severity.Warning);
                return new ValidationResult { IsValid = false, ErrorMessage = errorMessage };
            }
        }
        
        return new ValidationResult { IsValid = true, ErrorMessage = null };
    }
} 