using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using works.mmzk.PropertyTree;

namespace works.mmzk.PropertySystem
{
    public class UIPropertyInspector : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<UIPropertyInspector, UxmlTraits>
        {
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            // ここでUXMLから受け取りたい属性があれば定義できる（今回はなし）

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                // ここでは特に何もしない。構築はコードから行う
            }
        }

        private PropertyGroup _rootGroup;
        private TextField _searchField;
        private VisualElement _propertiesContainer;

        public void BindProperties(IEnumerable<IProperty> properties)
        {
            Clear();
            
            // 検索フィールドを追加
            CreateSearchField();
            
            // プロパティコンテナを作成
            _propertiesContainer = new VisualElement();
            Add(_propertiesContainer);
            
            // プロパティを処理
            ProcessProperties(properties, _propertiesContainer);
        }

        public void BindPropertyGroup(PropertyGroup rootGroup)
        {
            _rootGroup = rootGroup;
            Clear();
            
            // 検索フィールドを追加
            CreateSearchField();
            
            // プロパティコンテナを作成
            _propertiesContainer = new VisualElement();
            Add(_propertiesContainer);
            
            // プロパティを処理
            ProcessProperties(rootGroup.Items, _propertiesContainer);
        }

        private void CreateSearchField()
        {
            var searchContainer = new VisualElement();
            searchContainer.style.flexDirection = FlexDirection.Row;
            searchContainer.style.marginBottom = 8;
            
            _searchField = new TextField("パス検索");
            _searchField.style.flexGrow = 1;
            _searchField.style.marginRight = 8;
            
            var searchButton = new Button(() => PerformSearch())
            {
                text = "検索"
            };
            
            var clearButton = new Button(() => ClearSearch())
            {
                text = "クリア"
            };
            
            searchContainer.Add(_searchField);
            searchContainer.Add(searchButton);
            searchContainer.Add(clearButton);
            
            Add(searchContainer);
        }

        private void PerformSearch()
        {
            if (_rootGroup == null || string.IsNullOrEmpty(_searchField.value))
            {
                ClearSearch();
                return;
            }

            _propertiesContainer.Clear();
            
            var searchTerm = _searchField.value.Trim();
            List<IProperty> searchResults;

            if (searchTerm.Contains("*"))
            {
                // ワイルドカードパターン検索
                searchResults = _rootGroup.FindByPattern(searchTerm);
            }
            else if (searchTerm.EndsWith("."))
            {
                // プレフィックス検索
                searchResults = _rootGroup.FindByPrefix(searchTerm.TrimEnd('.'));
            }
            else
            {
                // 完全パス検索
                var result = _rootGroup.FindByPath(searchTerm);
                searchResults = result != null ? new List<IProperty> { result } : new List<IProperty>();
            }

            if (searchResults.Count > 0)
            {
                ProcessProperties(searchResults, _propertiesContainer);
            }
            else
            {
                var noResultLabel = new Label($"検索結果が見つかりません: {searchTerm}");
                noResultLabel.style.color = Color.red;
                _propertiesContainer.Add(noResultLabel);
            }
        }

        private void ClearSearch()
        {
            if (_rootGroup != null)
            {
                _propertiesContainer.Clear();
                ProcessProperties(_rootGroup.Items, _propertiesContainer);
            }
            _searchField.value = "";
        }

        private void ProcessProperties(IEnumerable<IProperty> properties, VisualElement parent)
        {
            foreach (var prop in properties)
            {
                var container = new VisualElement();
                container.style.flexDirection = FlexDirection.Column;
                container.style.marginBottom = 8;

                // FullPath拡張メソッドを使用して完全なパスを表示
                var fullPath = prop.GetFullPath();
                var label = new Label($"{prop.Name} ({fullPath})");
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                
                // 階層レベルに応じてインデントを調整
                var depth = prop.GetDepth();
                label.style.marginLeft = depth * 16;
                
                container.Add(label);

                switch (prop)
                {
                    case PropertyGroup group:
                        // PropertyGroupの場合は再帰的に処理
                        var groupContainer = new VisualElement();
                        groupContainer.style.marginLeft = 16; // インデント
                        groupContainer.style.borderLeftWidth = 2;
                        groupContainer.style.borderLeftColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                        groupContainer.style.paddingLeft = 8;

                        ProcessProperties(group.Items, groupContainer);
                        container.Add(groupContainer);
                        break;

                    case FloatPropertyAdapter p:
                        var slider = new Slider(p.Min, p.Max)
                        {
                            value = p.Get()
                        };

                        slider.RegisterValueChangedCallback(evt => p.Set(evt.newValue));

                        container.Add(slider);
                        break;

                    case BoolPropertyAdapter p:
                        var toggle = new Toggle()
                        {
                            value = p.Get()
                        };

                        toggle.RegisterValueChangedCallback(evt => p.Set(evt.newValue));

                        container.Add(toggle);
                        break;

                    case BoolProperty p:
                        var boolToggle = new Toggle()
                        {
                            value = p.Get()
                        };

                        boolToggle.RegisterValueChangedCallback(evt => p.Set(evt.newValue));

                        container.Add(boolToggle);
                        break;

                    case ActionProperty p:
                        var button = new Button(() => p.Execute())
                        {
                            text = p.Name
                        };
                        button.style.marginTop = 4;

                        container.Add(button);
                        break;

                    default:
                        // Enumプロパティの処理（リフレクションを使用）
                        if (ProcessEnumProperty(prop, container))
                        {
                            // Enumプロパティとして処理された場合は何もしない
                        }
                        else
                        {
                            // その他のプロパティの場合は何もしない
                        }
                        break;
                }

                parent.Add(container);
            }
        }

        private bool ProcessEnumProperty(IProperty prop, VisualElement container)
        {
            // リフレクションを使用してEnumプロパティかどうかを判定
            var propType = prop.GetType();
            
            // EnumProperty<T>またはEnumPropertyAdapter<T>かどうかをチェック
            if (propType.IsGenericType)
            {
                var genericTypeDef = propType.GetGenericTypeDefinition();
                if (genericTypeDef == typeof(EnumProperty<>) || genericTypeDef == typeof(EnumPropertyAdapter<>))
                {
                    var enumType = propType.GetGenericArguments()[0];
                    if (enumType.IsEnum)
                    {
                        CreateEnumDropdown(prop, enumType, container);
                        return true;
                    }
                }
            }
            
            return false;
        }

        private void CreateEnumDropdown(IProperty prop, System.Type enumType, VisualElement container)
        {
            var dropdown = new DropdownField();
            
            // Enumの値を取得
            var enumValues = System.Enum.GetValues(enumType).Cast<object>().ToArray();
            var enumNames = System.Enum.GetNames(enumType);
            
            // ドロップダウンの選択肢を設定
            var choices = new List<string>(enumNames);
            dropdown.choices = choices;
            
            // 現在の値を設定
            var currentValue = GetEnumPropertyValue(prop);
            var currentIndex = System.Array.IndexOf(enumValues, currentValue);
            if (currentIndex >= 0)
            {
                dropdown.value = enumNames[currentIndex];
            }
            
            // 値変更時のコールバックを設定
            dropdown.RegisterValueChangedCallback(evt =>
            {
                var selectedIndex = choices.IndexOf(evt.newValue);
                if (selectedIndex >= 0)
                {
                    var selectedValue = enumValues[selectedIndex];
                    SetEnumPropertyValue(prop, selectedValue);
                }
            });
            
            container.Add(dropdown);
        }

        private object GetEnumPropertyValue(IProperty prop)
        {
            // リフレクションを使用してGet()メソッドを呼び出し
            var getMethod = prop.GetType().GetMethod("Get");
            if (getMethod != null)
            {
                return getMethod.Invoke(prop, null);
            }
            return null;
        }

        private void SetEnumPropertyValue(IProperty prop, object value)
        {
            // リフレクションを使用してSet()メソッドを呼び出し
            var setMethod = prop.GetType().GetMethod("Set");
            if (setMethod != null)
            {
                setMethod.Invoke(prop, new object[] { value });
            }
        }
    }
}
