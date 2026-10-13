using System.Collections.Generic;
using CoachMaster.App;
using UnityEngine;

namespace CoachMaster.UI
{
    public sealed class ScreenRouter
    {
        readonly Transform _root;
        readonly GameSession _session;
        readonly Dictionary<string, GameScreen> _screens = new Dictionary<string, GameScreen>();
        GameScreen _current;

        public GameSession Session => _session;

        public ScreenRouter(Transform root, GameSession session)
        {
            _root = root;
            _session = session;
        }

        public void Register(string id, GameScreen screen)
        {
            _screens[id] = screen;
        }

        public void Show(string id)
        {
            if (_current != null)
                _current.Hide();

            _current = _screens[id];
            _current.Show(_root, this);
        }
    }

    public abstract class GameScreen
    {
        protected GameObject Root { get; private set; }

        public void Show(Transform parent, ScreenRouter router)
        {
            Root = new GameObject(GetType().Name, typeof(RectTransform));
            var rect = Root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            UiFactory.Stretch(rect);
            Build(router);
        }

        public void Hide()
        {
            if (Root != null)
                Object.Destroy(Root);
            Root = null;
        }

        protected abstract void Build(ScreenRouter router);
    }
}
