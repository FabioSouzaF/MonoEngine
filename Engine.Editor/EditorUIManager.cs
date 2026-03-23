using System.Collections.Generic;
using Engine.Editor.UI;

namespace Engine.Editor
{
    public class EditorUIManager
    {
        // A nossa lista dinâmica de painéis!
        private List<EditorWindow> _windows = new List<EditorWindow>();

        public void AddWindow(EditorWindow window)
        {
            _windows.Add(window);
        }

        public void Draw()
        {
            // O loop mágico: quem estiver na lista e aberto, é desenhado
            foreach (var window in _windows)
            {
                if (window.IsOpen)
                {
                    window.Draw();
                }
            }
        }
    }
}