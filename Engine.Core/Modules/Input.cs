using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace Engine.Core;

public static class Input
{
    private static KeyboardState _currentKeyState;
    private static KeyboardState _previousKeyState;
    
    // Adicionar MouseState futuramente aqui

    public static void Update()
    {
        _previousKeyState = _currentKeyState;
        _currentKeyState = Keyboard.GetState();
    }

    /// <summary>Retorna true enquanto a tecla estiver sendo segurada.</summary>
    public static bool GetKey(Keys key)
    {
        return _currentKeyState.IsKeyDown(key);
    }

    /// <summary>Retorna true apenas no frame que a tecla foi pressionada.</summary>
    public static bool GetKeyDown(Keys key)
    {
        return _currentKeyState.IsKeyDown(key) && !_previousKeyState.IsKeyDown(key);
    }

    /// <summary>Retorna true apenas no frame que a tecla foi solta.</summary>
    public static bool GetKeyUp(Keys key)
    {
        return !_currentKeyState.IsKeyDown(key) && _previousKeyState.IsKeyDown(key);
    }

    /// <summary>
    /// Retorna um valor de eixo (-1 a 1) para movimento.
    /// Ex: GetAxis("Horizontal", Keys.A, Keys.D)
    /// </summary>
    public static float GetAxis(string axisName, Keys negative, Keys positive)
    {
        float value = 0f;
        if (GetKey(positive)) value += 1f;
        if (GetKey(negative)) value -= 1f;
        return value;
    }
}