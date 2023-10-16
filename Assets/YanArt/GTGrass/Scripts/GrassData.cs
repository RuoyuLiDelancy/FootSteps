using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GTGrass
{
    [CreateAssetMenu(fileName = "Grass Data", menuName = "Grass/Grass Data", order = 1)]
    public class GrassData : ScriptableObject
    {
        public Mesh m_Mesh;
        [HideInInspector] public List<Cell> m_CellList;
    }

    [Serializable]
    public class Cell
    {
        public Vector3 pos;
        public List<VertexData> vertices;

        public Cell() { }

        public Cell(Vector3 _pos, List<VertexData> _vert)
        {
            pos = _pos;
            vertices = _vert;
        }

        public void Populate(Vector3 _pos, List<VertexData> _vert)
        {
            pos = _pos;
            vertices = _vert;
        }
    }

    [Serializable]
    public class VertexData : IComparable<VertexData>
    {
        public int index;
        public Vector3 pos;
        public List<int> triangleIndxList;

        public VertexData(int _index, Vector3 _pos, List<int> _trisIndx)
        {
            index = _index;
            pos = _pos;
            triangleIndxList = _trisIndx;
        }

        // Default comparer for Part type.
        public int CompareTo(VertexData compareVertex)
        {
            // A null value means that this object is greater.
            if (compareVertex == null)
                return 1;
            else
                return this.index.CompareTo(compareVertex.index);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (!(obj is VertexData)) return false;

            return this.index == ((VertexData)obj).index && this.pos == ((VertexData)obj).pos;
        }

        public override int GetHashCode()
        {
            string combined = index.ToString() + pos.ToString();
            return combined.GetHashCode();
        }
    }

}
