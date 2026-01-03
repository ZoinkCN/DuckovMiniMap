using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ZoinkModdingLibrary.GameUI
{
    public static class UIElements
    {
        public static bool CreateFilledRectTransform(Transform parent, string objectName, out GameObject? gameObject, out RectTransform? rectTransform)
        {
            try
            {
                if (string.IsNullOrEmpty(objectName))
                {
                    objectName = "NewGameObject";
                }
                gameObject = new GameObject(objectName);
                rectTransform = gameObject.AddComponent<RectTransform>();

                rectTransform.SetParent(parent);
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                return true;
            }
            catch (Exception)
            {
                gameObject = null;
                rectTransform = null;
                return false;
            }
        }
    }
}
