﻿using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Counter_Strike_multihack
{
    public class Entity
    {
        public IntPtr address { get; set; }

        public int health { get; set; }

        public int teamNum { get; set; }

        public int jumpFlag { get; set; }

        public Vector3 origin { get; set; }

        public Vector3 abs { get; set; }

        public Vector3 viewOffset { get; set; } 

        public Vector2 originScreenPosition { get; set; }

        public Vector2 absScreenPosition { get; set; }
    }
}
