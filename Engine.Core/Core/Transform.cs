using Engine.Core.Components;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace Engine.Core.Entities;

public class Transform : Component
{
    // --- DADOS LOCAIS (SERIALIZADOS) ---
    // Estes são os únicos dados salvos no JSON.

    public Vector3 LocalPosition { get; set; } = Vector3.Zero;
    // A Mágica da Unity: Guardamos os ângulos reais para o Inspetor e para o JSON!
    public Vector3 LocalEulerAngles = Vector3.Zero;

    // O Quaternion agora é gerado automaticamente a partir dos ângulos.
    // O JSON ignora isso para não salvar dados duplicados!
    [Newtonsoft.Json.JsonIgnore]
    public Microsoft.Xna.Framework.Quaternion LocalRotation
    {
        get
        {
            float radX = Microsoft.Xna.Framework.MathHelper.ToRadians(LocalEulerAngles.X);
            float radY = Microsoft.Xna.Framework.MathHelper.ToRadians(LocalEulerAngles.Y);
            float radZ = Microsoft.Xna.Framework.MathHelper.ToRadians(LocalEulerAngles.Z);

            // 1. Criamos os três Quaternions puros, um para cada eixo
            var qX = Microsoft.Xna.Framework.Quaternion.CreateFromAxisAngle(Microsoft.Xna.Framework.Vector3.UnitX, radX);
            var qY = Microsoft.Xna.Framework.Quaternion.CreateFromAxisAngle(Microsoft.Xna.Framework.Vector3.UnitY, radY);
            var qZ = Microsoft.Xna.Framework.Quaternion.CreateFromAxisAngle(Microsoft.Xna.Framework.Vector3.UnitZ, radZ);

            // 2. A ORDEM DE OURO DO 2D (Euler Order):
            // Aplicamos X e Y (Flip e Inclinação local) e aplicamos o Z (Giro na Tela) por último!
            return qX * qY * qZ; 
        }
        set { }
    }
    
    public Vector3 LocalScale { get; set; } = Vector3.One;

    // --- PROPRIEDADES GLOBAIS (CALCULADAS) ---
    // Adicionamos [JsonIgnore] para o Serializador NÃO tentar escrever aqui e causar crash.
    
    [JsonIgnore]
    public Vector3 Position
    {
        get
        {
            return WorldMatrix.Translation;
        }
        set
        {
            // Se tiver pai, precisamos converter a posição global desejada para o espaço local do pai
            if (GameObject?.Parent != null)
            {
                var parentMatrix = GameObject.Parent.Transform.WorldMatrix;
                var inverseParent = Matrix.Invert(parentMatrix);
                LocalPosition = Vector3.Transform(value, inverseParent);
            }
            else
            {
                LocalPosition = value;
            }
        }
    }

    [JsonIgnore]
    public Matrix WorldMatrix
    {
        get
        {
            // Ordem de multiplicação em 3D: Scale * Rotation * Translation
            var localMatrix = Matrix.CreateScale(LocalScale) *
                              Matrix.CreateFromQuaternion(LocalRotation) *
                              Matrix.CreateTranslation(LocalPosition);

            if (GameObject?.Parent != null)
            {
                return localMatrix * GameObject.Parent.Transform.WorldMatrix;
            }

            return localMatrix;
        }
    }
    
    

    // Atalhos úteis para movimentação 3D
    public void Translate(Vector3 translation)
    {
        LocalPosition += translation;
    }

    public void Rotate(Vector3 axis, float angle)
    {
        LocalRotation *= Quaternion.CreateFromAxisAngle(axis, angle);
    }
}