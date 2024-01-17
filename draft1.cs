using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class Program
{
    static void Main(string[] args)
    {
        string appConfigPath = "path/to/your/App.config"; // Замените на путь к вашему App.config
        string appSettingsJsonPath = "path/to/your/appsettings.json"; // Путь для сохранения appsettings.json

        try
        {
            var doc = new XmlDocument();
            doc.Load(appConfigPath);
            string jsonText = JsonConvert.SerializeXmlNode(doc);

            var jObject = JObject.Parse(jsonText);

            // Обработка тегов
            ProcessTags(jObject);

            // Удаление символов '@' из названий свойств
            RemoveAtSignsFromPropertyNames(jObject);

            string formattedJson = JsonConvert.SerializeObject(jObject, Formatting.Indented);
            File.WriteAllText(appSettingsJsonPath, formattedJson);
            Console.WriteLine("Конвертация завершена успешно.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка при конвертации: " + ex.Message);
        }
    }

    private static void ProcessTags(JToken token)
    {
        if (token.Type == JTokenType.Object)
        {
            var obj = (JObject)token;
            var propertiesToRemove = new List<string>();
            foreach (var property in obj.Properties())
            {
                if (property.Value is JObject || property.Value is JArray)
                {
                    if (property.Value["section"] != null || property.Value["add"] != null)
                    {
                        ConvertToDesiredFormat(property.Name, property.Value);
                        propertiesToRemove.Add(property.Name);
                    }
                    else
                    {
                        ProcessTags(property.Value);
                    }
                }
            }

            foreach (var propName in propertiesToRemove)
            {
                obj.Remove(propName);
            }
        }
        else if (token.Type == JTokenType.Array)
        {
            foreach (var item in token.Children())
            {
                ProcessTags(item);
            }
        }
    }

    private static void ConvertToDesiredFormat(string tagName, JToken token)
    {
        var resultObject = new JObject();
        var items = token["section"] ?? token["add"];

        if (items is JArray)
        {
            foreach (var item in items)
            {
                string name = item["@name"].ToString();
                string value = item["@" + tagName] ?? item["@" + Singularize(tagName)];
                resultObject[name] = value;
            }
        }
        else if (items is JObject)
        {
            string name = items["@name"].ToString();
            string value = items["@" + tagName] ?? items["@" + Singularize(tagName)];
            resultObject[name] = value;
        }

        token.Parent[tagName] = resultObject;
    }

    private static string Singularize(string word)
    {
        // Простое сокращение до единственного числа
        if (word.EndsWith("s"))
        {
            return word.Substring(0, word.Length - 1);
        }
        return word;
    }

    private static void RemoveAtSignsFromPropertyNames(JToken token)
    {
        if (token.Type == JTokenType.Object)
        {
            var obj = (JObject)token;
            foreach (var property in obj.ToList())
            {
                if (property.Key.StartsWith("@"))
                {
                    var value = property.Value;
                    obj.Remove(property.Key);
                    obj.Add(property.Key.TrimStart('@'), value);
                }

                RemoveAtSignsFromPropertyNames(property.Value);
            }
        }
        else if (token.Type == JTokenType.Array)
        {
            foreach (var item in token.Children())
            {
                RemoveAtSignsFromPropertyNames(item);
            }
        }
    }
}
