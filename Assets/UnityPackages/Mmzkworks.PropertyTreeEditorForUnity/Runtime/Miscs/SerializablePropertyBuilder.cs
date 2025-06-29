using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using works.mmzk.PropertyTree;

namespace works.mmzk.PropertySystem.Utilities
{
    /// <summary>
    /// UnityのSerializableクラスやSerializedFieldから自動でPropertyGroup構成を構築するUtilityクラス
    /// </summary>
    public static class SerializablePropertyBuilder
    {
        /// <summary>
        /// オブジェクトからPropertyGroupを構築
        /// </summary>
        /// <param name="target">対象オブジェクト</param>
        /// <param name="groupName">グループ名</param>
        /// <param name="options">構築オプション</param>
        /// <returns>構築されたPropertyGroup</returns>
        public static PropertyGroup BuildFromObject(object target, string groupName = "Root", BuildOptions options = null)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            options ??= new BuildOptions();
            var rootGroup = new PropertyGroup(groupName);
            
            BuildFromObjectInternal(target, rootGroup, options);
            
            return rootGroup;
        }

        /// <summary>
        /// MonoBehaviourからPropertyGroupを構築
        /// </summary>
        /// <param name="monoBehaviour">対象のMonoBehaviour</param>
        /// <param name="groupName">グループ名</param>
        /// <param name="options">構築オプション</param>
        /// <returns>構築されたPropertyGroup</returns>
        public static PropertyGroup BuildFromMonoBehaviour(MonoBehaviour monoBehaviour, string groupName = "Root", BuildOptions options = null)
        {
            if (monoBehaviour == null)
                throw new ArgumentNullException(nameof(monoBehaviour));

            options ??= new BuildOptions();
            var rootGroup = new PropertyGroup(groupName);
            
            BuildFromObjectInternal(monoBehaviour, rootGroup, options);
            
            return rootGroup;
        }

        private static void BuildFromObjectInternal(object target, PropertyGroup parentGroup, BuildOptions options)
        {
            var type = target.GetType();
            var fields = GetSerializableFields(type, options);

            foreach (var field in fields)
            {
                try
                {
                    var property = CreatePropertyFromField(target, field, options);
                    if (property != null)
                    {
                        parentGroup.Add(property);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to create property for field {field.Name}: {ex.Message}");
                }
            }
        }

        private static IEnumerable<FieldInfo> GetSerializableFields(Type type, BuildOptions options)
        {
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var fields = type.GetFields(bindingFlags);

            return fields.Where(field =>
            {
                // SerializeField属性があるフィールド
                if (field.GetCustomAttribute<SerializeField>() != null)
                    return true;

                // パブリックフィールド
                if (field.IsPublic)
                    return true;

                // 除外リストに含まれているフィールドは除外
                if (options.ExcludeFields.Contains(field.Name))
                    return false;

                // 除外する型のフィールドは除外
                if (options.ExcludeTypes.Contains(field.FieldType))
                    return false;

                return true;
            });
        }

        private static IProperty CreatePropertyFromField(object target, FieldInfo field, BuildOptions options)
        {
            var fieldType = field.FieldType;
            var fieldName = GetDisplayName(field);

            // 基本型の処理
            if (fieldType == typeof(float))
            {
                return CreateFloatProperty(target, field, fieldName, options);
            }
            else if (fieldType == typeof(int))
            {
                return CreateIntProperty(target, field, fieldName, options);
            }
            else if (fieldType == typeof(bool))
            {
                return CreateBoolProperty(target, field, fieldName);
            }
            else if (fieldType == typeof(string))
            {
                return CreateStringProperty(target, field, fieldName);
            }
            else if (fieldType == typeof(Vector2))
            {
                return CreateVector2Property(target, field, fieldName, options);
            }
            else if (fieldType == typeof(Vector3))
            {
                return CreateVector3Property(target, field, fieldName, options);
            }
            else if (fieldType == typeof(Color))
            {
                return CreateColorProperty(target, field, fieldName);
            }
            else if (fieldType.IsEnum)
            {
                return CreateEnumProperty(target, field, fieldName);
            }
            else if (fieldType.IsClass && !fieldType.IsArray && !fieldType.IsGenericType)
            {
                // ネストしたオブジェクトの処理
                var nestedValue = field.GetValue(target);
                if (nestedValue != null && ShouldCreateNestedGroup(field, options))
                {
                    var nestedGroup = new PropertyGroup(fieldName);
                    BuildFromObjectInternal(nestedValue, nestedGroup, options);
                    return nestedGroup;
                }
            }

            return null;
        }

        private static string GetDisplayName(FieldInfo field)
        {
            var headerAttr = field.GetCustomAttribute<HeaderAttribute>();
            if (headerAttr != null)
            {
                return headerAttr.header;
            }

            var tooltipAttr = field.GetCustomAttribute<TooltipAttribute>();
            if (tooltipAttr != null)
            {
                return tooltipAttr.tooltip;
            }

            return field.Name;
        }

        private static FloatPropertyAdapter CreateFloatProperty(object target, FieldInfo field, string name, BuildOptions options)
        {
            var rangeAttr = field.GetCustomAttribute<RangeAttribute>();
            var min = rangeAttr?.min ?? options.DefaultFloatMin;
            var max = rangeAttr?.max ?? options.DefaultFloatMax;

            return new FloatPropertyAdapter(name, (float)min, (float)max,
                () => (float)field.GetValue(target),
                value => field.SetValue(target, value));
        }

        private static IntPropertyAdapter CreateIntProperty(object target, FieldInfo field, string name, BuildOptions options)
        {
            var rangeAttr = field.GetCustomAttribute<RangeAttribute>();
            var min = rangeAttr?.min ?? options.DefaultIntMin;
            var max = rangeAttr?.max ?? options.DefaultIntMax;

            return new IntPropertyAdapter(name, (int)min, (int)max,
                () => (int)field.GetValue(target),
                value => field.SetValue(target, value));
        }

        private static BoolPropertyAdapter CreateBoolProperty(object target, FieldInfo field, string name)
        {
            return new BoolPropertyAdapter(name,
                () => (bool)field.GetValue(target),
                value => field.SetValue(target, value));
        }

        private static StringPropertyAdapter CreateStringProperty(object target, FieldInfo field, string name)
        {
            return new StringPropertyAdapter(name,
                () => (string)field.GetValue(target),
                value => field.SetValue(target, value));
        }

        private static PropertyGroup CreateVector2Property(object target, FieldInfo field, string name, BuildOptions options)
        {
            var group = new PropertyGroup(name);
            
            group.Add(new FloatPropertyAdapter("X", options.DefaultFloatMin, options.DefaultFloatMax,
                () => ((Vector2)field.GetValue(target)).x,
                value => {
                    var vector = (Vector2)field.GetValue(target);
                    vector.x = value;
                    field.SetValue(target, vector);
                }));

            group.Add(new FloatPropertyAdapter("Y", options.DefaultFloatMin, options.DefaultFloatMax,
                () => ((Vector2)field.GetValue(target)).y,
                value => {
                    var vector = (Vector2)field.GetValue(target);
                    vector.y = value;
                    field.SetValue(target, vector);
                }));

            return group;
        }

        private static PropertyGroup CreateVector3Property(object target, FieldInfo field, string name, BuildOptions options)
        {
            var group = new PropertyGroup(name);
            
            group.Add(new FloatPropertyAdapter("X", options.DefaultFloatMin, options.DefaultFloatMax,
                () => ((Vector3)field.GetValue(target)).x,
                value => {
                    var vector = (Vector3)field.GetValue(target);
                    vector.x = value;
                    field.SetValue(target, vector);
                }));

            group.Add(new FloatPropertyAdapter("Y", options.DefaultFloatMin, options.DefaultFloatMax,
                () => ((Vector3)field.GetValue(target)).y,
                value => {
                    var vector = (Vector3)field.GetValue(target);
                    vector.y = value;
                    field.SetValue(target, vector);
                }));

            group.Add(new FloatPropertyAdapter("Z", options.DefaultFloatMin, options.DefaultFloatMax,
                () => ((Vector3)field.GetValue(target)).z,
                value => {
                    var vector = (Vector3)field.GetValue(target);
                    vector.z = value;
                    field.SetValue(target, vector);
                }));

            return group;
        }

        private static PropertyGroup CreateColorProperty(object target, FieldInfo field, string name)
        {
            var group = new PropertyGroup(name);
            
            group.Add(new FloatPropertyAdapter("R", 0f, 1f,
                () => ((Color)field.GetValue(target)).r,
                value => {
                    var color = (Color)field.GetValue(target);
                    color.r = value;
                    field.SetValue(target, color);
                }));

            group.Add(new FloatPropertyAdapter("G", 0f, 1f,
                () => ((Color)field.GetValue(target)).g,
                value => {
                    var color = (Color)field.GetValue(target);
                    color.g = value;
                    field.SetValue(target, color);
                }));

            group.Add(new FloatPropertyAdapter("B", 0f, 1f,
                () => ((Color)field.GetValue(target)).b,
                value => {
                    var color = (Color)field.GetValue(target);
                    color.b = value;
                    field.SetValue(target, color);
                }));

            group.Add(new FloatPropertyAdapter("A", 0f, 1f,
                () => ((Color)field.GetValue(target)).a,
                value => {
                    var color = (Color)field.GetValue(target);
                    color.a = value;
                    field.SetValue(target, color);
                }));

            return group;
        }

        private static IProperty CreateEnumProperty(object target, FieldInfo field, string name)
        {
            var enumType = field.FieldType;
            var genericType = typeof(EnumPropertyAdapter<>).MakeGenericType(enumType);
            
            return (IProperty)Activator.CreateInstance(genericType, name,
                new Func<object>(() => field.GetValue(target)),
                new Action<object>(value => field.SetValue(target, value)));
        }

        private static bool ShouldCreateNestedGroup(FieldInfo field, BuildOptions options)
        {
            // 除外する型のチェック
            if (options.ExcludeTypes.Contains(field.FieldType))
                return false;

            // 除外するフィールド名のチェック
            if (options.ExcludeFields.Contains(field.Name))
                return false;

            // ネストしたオブジェクトの作成を許可するかどうか
            return options.AllowNestedObjects;
        }

        /// <summary>
        /// 構築オプション
        /// </summary>
        public class BuildOptions
        {
            /// <summary>
            /// 除外するフィールド名のリスト
            /// </summary>
            public HashSet<string> ExcludeFields { get; set; } = new HashSet<string>();

            /// <summary>
            /// 除外する型のリスト
            /// </summary>
            public HashSet<Type> ExcludeTypes { get; set; } = new HashSet<Type>();

            /// <summary>
            /// ネストしたオブジェクトの作成を許可するかどうか
            /// </summary>
            public bool AllowNestedObjects { get; set; } = true;

            /// <summary>
            /// float型のデフォルト最小値
            /// </summary>
            public float DefaultFloatMin { get; set; } = 0f;

            /// <summary>
            /// float型のデフォルト最大値
            /// </summary>
            public float DefaultFloatMax { get; set; } = 1f;

            /// <summary>
            /// int型のデフォルト最小値
            /// </summary>
            public int DefaultIntMin { get; set; } = 0;

            /// <summary>
            /// int型のデフォルト最大値
            /// </summary>
            public int DefaultIntMax { get; set; } = 100;

            public BuildOptions()
            {
                // デフォルトで除外する型
                ExcludeTypes.Add(typeof(MonoBehaviour));
                ExcludeTypes.Add(typeof(Component));
                ExcludeTypes.Add(typeof(UnityEngine.Object));
                ExcludeTypes.Add(typeof(GameObject));
                ExcludeTypes.Add(typeof(Transform));
            }
        }
    }
} 