using Engine.Core.Entities;
using Engine.Core.Components;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Security.Cryptography;
using Microsoft.Xna.Framework;

namespace Engine.Core.Serialization;

public class EngineSerializationBinder : DefaultSerializationBinder
{
    public override Type BindToType(string assemblyName, string typeName)
    {
        string justClassName = typeName.Contains(".") ? typeName.Substring(typeName.LastIndexOf('.') + 1) : typeName;

        // 1. A NOVA REGRA DE OURO: Procurar na memória RAM PRIMEIRO!
        // Assim impedimos o Newtonsoft de sequer tentar procurar a DLL "EditorScripts" e dar crash
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types = null;
            try { types = assembly.GetTypes(); }
            catch (System.Reflection.ReflectionTypeLoadException e) { types = e.Types; }
            catch { continue; }

            if (types != null)
            {
                foreach (var type in types)
                {
                    if (type != null && (type.Name == justClassName || type.FullName == typeName))
                    {
                        return type; // Achou na RAM! Retorna imediatamente e cancela o erro do JSON!
                    }
                }
            }
        }

        // 2. Se não achou na RAM, deixamos o Newtonsoft tentar o método padrão (para variáveis nativas do C#)
        try
        {
            return base.BindToType(assemblyName, typeName);
        }
        catch
        {
            Console.WriteLine($"[AVISO] Classe não resolvida no JSON: {typeName}");
            return null;
        }
    }

    public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
    {
        base.BindToName(serializedType, out assemblyName, out typeName);
        
        // Garante que a cena seja salva limpa no futuro
        if (assemblyName == "EditorScripts" || assemblyName == "UserScripts")
        {
            assemblyName = null; 
        }
    }
}

public static class SceneSerializer
{
    private static JsonSerializerSettings GetSettings()
    {
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Auto,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            
            // --- A MÁGICA QUE SALVA A ENGINE ---
            // Força o JSON a substituir as listas/arrays em vez de somar os itens duplicados!
            ObjectCreationHandling = ObjectCreationHandling.Replace, 
            
            SerializationBinder = new EngineSerializationBinder(),
            Error = delegate(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
            {
                Console.WriteLine($"[JSON IGNORADO] Erro tolerado: {args.ErrorContext.Error.Message}");
                args.ErrorContext.Handled = true; 
            }
        };

        settings.Converters.Add(new Vector3Converter());
        settings.Converters.Add(new ColorConverter());
        settings.Converters.Add(new QuaternionConverter()); 

        return settings;
    }

    private static void ReconstructReferences(GameObject obj)
    {
        foreach (var component in obj.Components.ToList())
        {
            component.Attach(obj);
            if (component is Transform t)
            {
                obj.AddComponent(t); 
            }
        }

        foreach (var child in obj.Children)
        {
            child.Parent = obj;
            ReconstructReferences(child);
        }
    }

    // --- NOVIDADE (O DESFIBRILADOR): Acorda os componentes DEPOIS que a cena inteira foi montada ---
    private static void StartAllComponents(GameObject obj)
    {
        foreach (var component in obj.Components)
        {
            component.Start();
        }
        foreach (var child in obj.Children)
        {
            StartAllComponents(child);
        }
    }

    public static string Serialize(Scene scene)
    {
        return JsonConvert.SerializeObject(scene, GetSettings());
    }

    public static Scene Deserialize(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        var scene = JsonConvert.DeserializeObject<Scene>(json, GetSettings());

        if (scene != null)
        {
            // 1. Conecta os pais e filhos
            foreach (var rootObj in scene.RootObjects) ReconstructReferences(rootObj);
            
            // 2. Roda o Start() de todos os scripts (como a Unity faz no Awake/Start)
            foreach (var rootObj in scene.RootObjects) StartAllComponents(rootObj);
            
            scene.OnAfterDeserialize(); 
        }
        return scene;
    }

    public static void SaveToFile(Scene scene, string filePath)
    {
        string json = JsonConvert.SerializeObject(scene, GetSettings());
        File.WriteAllText(filePath, json);
    }

    public static Scene LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        string json = File.ReadAllText(filePath);
        var scene = JsonConvert.DeserializeObject<Scene>(json, GetSettings());

        if (scene != null)
        {
            foreach (var rootObj in scene.RootObjects) ReconstructReferences(rootObj);
            foreach (var rootObj in scene.RootObjects) StartAllComponents(rootObj);
            scene.OnAfterDeserialize();
        }
        return scene;
    }
    
    // ==========================================
    // CRIPTOGRAFIA DE BUILD (AES-256)
    // ==========================================
    
    // Uma chave secreta de 32 bytes e um vetor de 16 bytes. 
    // Coloquei o seu nome na chave para batizar a segurança da sua Engine! ;)
    private static readonly byte[] CryptoKey = System.Text.Encoding.UTF8.GetBytes("F4b10M0n0Eng1n3S3cr3tK3y12345678"); 
    private static readonly byte[] CryptoIV = System.Text.Encoding.UTF8.GetBytes("F4b101n1tV3ct0r1");

    // Usado APENAS pelo MainMenuBar na hora de exportar o jogo
    public static void ExportEncrypted(string json, string filePath)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = CryptoKey;
            aes.IV = CryptoIV;
            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (StreamWriter sw = new StreamWriter(cs))
                {
                    sw.Write(json);
                }
                // Salva o resultado como um arquivo binário completamente ilegível
                File.WriteAllBytes(filePath, ms.ToArray());
            }
        }
    }

    // Usado APENAS pelo Game1.cs no Runtime para ler a fase
    public static Scene LoadEncrypted(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        
        byte[] cipherText = File.ReadAllBytes(filePath);
        string json;

        using (Aes aes = Aes.Create())
        {
            aes.Key = CryptoKey;
            aes.IV = CryptoIV;
            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            
            using (MemoryStream ms = new MemoryStream(cipherText))
            using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (StreamReader sr = new StreamReader(cs))
            {
                json = sr.ReadToEnd();
            }
        }
        
        // Passa o JSON limpo que estava na memória para o nosso desserializador normal
        return Deserialize(json);
    }
    
}