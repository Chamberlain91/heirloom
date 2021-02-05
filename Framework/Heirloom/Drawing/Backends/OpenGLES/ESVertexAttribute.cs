using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

using Heirloom.Mathematics;

namespace Heirloom.Drawing.OpenGLES
{
    internal class ESVertexAttribute
    {
        #region Constructors

        public ESVertexAttribute(ESVertexAttributeName name, int size, VertexAttributeType type, bool normalized, uint offset = 0)
        {
            Name = name;
            Size = size;
            Type = type;

            Normalized = normalized;

            Index = GetAttributeIndex(name) + offset;
        }

        #endregion

        #region Properties

        public ESVertexAttributeName Name { get; }

        public VertexAttributeType Type { get; }

        public int Size { get; }

        public bool Normalized { get; }

        public uint Index { get; }

        #endregion

        internal void SetAttributePointer(int offset, int stride, int divisor)
        {
            GLES.EnableVertexAttribArray(Index);
            GLES.SetVertexAttribPointer(Index, Size, Type, Normalized, stride, (uint) offset);
            GLES.SetVertexAttribDivisor(Index, divisor);
        }

        #region Static Attribute Helpers

        private static readonly IReadOnlyDictionary<ESVertexAttributeName, uint> _attributeIndices;

        static ESVertexAttribute()
        {
            // Construct attribute index map
            var offset = 0u;
            var attributeIndices = new Dictionary<ESVertexAttributeName, uint>();
            foreach (ESVertexAttributeName name in Enum.GetValues(typeof(ESVertexAttributeName)))
            {
                attributeIndices[name] = offset;

                // Move 
                if (name == ESVertexAttributeName.Transform) { offset += 2; }
                else { offset += 1; }
            }

            // 
            _attributeIndices = attributeIndices;
        }

        public static ESVertexAttributeName[] GetAttributes()
        {
            return (ESVertexAttributeName[]) Enum.GetValues(typeof(ESVertexAttributeName));
        }

        public static uint GetAttributeIndex(ESVertexAttributeName name)
        {
            return _attributeIndices[name];
        }

        internal static int GetSizeInBytes(VertexAttributeType attrType, int count)
        {
            switch (attrType)
            {
                case VertexAttributeType.Byte:
                case VertexAttributeType.UnsignedByte:
                    return 1 * count;

                case VertexAttributeType.Short:
                case VertexAttributeType.UnsignedShort:
                case VertexAttributeType.HalfFloat:
                    return 2 * count;

                case VertexAttributeType.Int:
                case VertexAttributeType.UnsignedInt:
                case VertexAttributeType.Float:
                case VertexAttributeType.Fixed:
                    return 4 * count;
            }

            throw new InvalidOperationException($"Unknown vertex attribute type: {attrType}");
        }

        internal static ESVertexAttribute[] GenerateAttributes(Type type)
        {
            // Get all fields w/ attribute defs
            var fields = type.GetFields()
                .Where(x => x.GetCustomAttribute<ESVertexAttributeAttribute>() != null)
                .ToArray();

            // Must have at least one attribute defined
            if (fields.Length == 0)
            {
                throw new InvalidOperationException("Must have at least one field with a vertex attribute defined.");
            }

            // Console.WriteLine($"Attribute Layout: {type}");

            // Create attribute list (one attributed per field)
            var attributes = new List<ESVertexAttribute>();

            // For each field
            for (var f = 0; f < fields.Length; f++)
            {
                var attribute = fields[f].GetCustomAttribute<ESVertexAttributeAttribute>();
                var field = fields[f];

                // Number of attributes needed to describe this data, most attributes will be one.
                var attributeCount = GetAttributeCount(field.FieldType);

                // Get offset and usable size
                var nextOffset = f != (fields.Length - 1) ? OffsetOf(fields[f + 1]) : Marshal.SizeOf(type);
                var currOffset = OffsetOf(field);
                var size = nextOffset - currOffset;

                // Gather respective field type
                var attr = GetAttributeType(field.FieldType);
                var step = GetSizeInBytes(attr, 1) * attributeCount;
                var count = size / step;
                var waste = size % step;
                if (waste != 0) { throw new Exception($"Vertex attribute defined in struct has unattributed space after '{attr}'."); }

                // Store extracted attribute data
                for (var o = 0u; o < attributeCount; o++)
                {
                    var _attribute = new ESVertexAttribute(attribute.Attribute, count, attr, attribute.Normalize, o);
                    // Console.WriteLine($"  {attribute.Attribute} ({_attribute.Index}): {count} x {attr}");
                    attributes.Add(_attribute);
                }
            }

            return attributes.ToArray();

            static int OffsetOf(FieldInfo field)
            {
                return (int) Marshal.OffsetOf(field.DeclaringType, field.Name);
            }
        }

        private static VertexAttributeType GetAttributeType(Type type)
        {
            if (type == typeof(float) ||
                type == typeof(Color) ||
                type == typeof(Vector) ||
                type == typeof(Matrix) ||
                type == typeof(Rectangle) ||
                type == typeof(Size))
            {
                return VertexAttributeType.Float;
            }
            else
            if (type == typeof(int) ||
                type == typeof(IntVector) ||
                type == typeof(IntRectangle) ||
                type == typeof(IntSize))
            {
                return VertexAttributeType.Int;
            }
            //else
            //if (type == typeof(Half))
            //{
            //    return VertexAttributeType.HalfFloat;
            //}
            else
            if (type == typeof(uint))
            {
                return VertexAttributeType.UnsignedInt;
            }
            else
            if (type == typeof(short))
            {
                return VertexAttributeType.Short;
            }
            else
            if (type == typeof(ushort))
            {
                return VertexAttributeType.UnsignedShort;
            }
            else
            if (type == typeof(sbyte))
            {
                return VertexAttributeType.Byte;
            }
            else
            if (type == typeof(byte) || type == typeof(ColorBytes))
            {
                return VertexAttributeType.UnsignedByte;
            }

            throw new InvalidOperationException($"Unknown GLSL vertex attribute type associated with '{type}'");
        }

        private static int GetAttributeCount(Type type)
        {
            if (type == typeof(Matrix)) { return 2; }
            return 1;
        }

        #endregion

        public override string ToString()
        {
            return $"[{Index}] {Name}: {Size} x {Type} {(Normalized ? "(Normalized)" : "")}";
        }
    }
}
