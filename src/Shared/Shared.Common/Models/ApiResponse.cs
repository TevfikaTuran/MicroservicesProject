namespace Shared.Common.Models;

/// <summary>
/// Tüm mikroservisler tarafından kullanılan standart API yanıt modeli.
/// Single Responsibility Principle: Sadece API yanıt yapısını tanımlar.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> SuccessResult(T data, string message = "İşlem başarılı")
        => new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> FailResult(string message, List<string>? errors = null)
        => new() { Success = false, Message = message, Errors = errors ?? new() };
}
