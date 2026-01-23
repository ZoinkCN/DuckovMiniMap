using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MiniMap.Poi
{
    public struct CharacterPoiIconData
    {
        public CharacterPoiIconData() { }

        public Sprite? Icon = null;
        public Sprite? Arrow = null;
        public float IconScale = 1f;
        public float ArrowScale = 1f;
        public bool HideIcon = false;
        public bool HideArrow = false;
    }
}
