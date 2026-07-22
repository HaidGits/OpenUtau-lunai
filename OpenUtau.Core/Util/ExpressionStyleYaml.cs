using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenUtau.Core.Util;

/// <summary>
/// Saved Project Expressions snapshot ("singer style"), stored under ExpressionStyles/*.yaml.
/// </summary>
public class ExpressionStyleYaml {
    public string Name = "";
    /// <summary>Display name of the singer when the style was saved (not a path).</summary>
    public string SingerName = "";
    /// <summary>Expression abbr to project custom default value.</summary>
    public Dictionary<string, float> Values = new();

    public static ExpressionStyleYaml LoadFromFile(string path) {
        return Yaml.DefaultDeserializer.Deserialize<ExpressionStyleYaml>(
            File.ReadAllText(path, Encoding.UTF8)) ?? new ExpressionStyleYaml();
    }

    public void SaveToFile(string path) {
        File.WriteAllText(path, Yaml.DefaultSerializer.Serialize(this), Encoding.UTF8);
    }
}
