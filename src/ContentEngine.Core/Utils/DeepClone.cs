using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;

namespace ContentEngine.Core.Utils;

/// <summary>
/// 定义对象深拷贝扩展方法
/// </summary>
public static class CloneExtend
{
    public static T? DeepClone<T>(this T obj)
    {
        if (obj == null)
        {
            return default;
        }

        // 配置 JsonSerializer 选项
        // 对于复杂的对象，你可能需要配置PreserveReferences来处理循环引用
        var options = new JsonSerializerOptions
        {
            // 如果你的对象图中存在循环引用（例如，A.B.A），必须设置此选项
            // ReferenceHandler = ReferenceHandler.Preserve 
        };

        // 序列化为 JSON 字符串
        var jsonString = JsonSerializer.Serialize(obj, options);
        
        // 从 JSON 字符串反序列化回对象
        return JsonSerializer.Deserialize<T>(jsonString, options);
    }
}