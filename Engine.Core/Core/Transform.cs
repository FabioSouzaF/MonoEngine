using Engine.Core.Components;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace Engine.Core.Entities;

public class Transform : Component
{
    // --- DADOS LOCAIS (SERIALIZADOS) ---
    // Estes são os únicos dados salvos no JSON.

    public Vector3 LocalPosition { get; set; } = Vector3.Zero;
    public Quaternion LocalRotation { get; set; } = Quaternion.Identity;
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