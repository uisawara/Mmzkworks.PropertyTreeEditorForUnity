Unity Project, AppUI の Runtime用 PropertyInspector です。
専用のPropertySystemで定義したプロパティ群をそのままInspectorとしてアプリ内に表示・編集できるようにします。

<img src="C:\Users\yu-og\AppData\Roaming\Typora\typora-user-images\image-20250621131933703.png" alt="image-20250621131933703" style="zoom:50%;" />

## Installation

upm

```
git@github.com:uisawara/Mmzkworks.PropertyTreeEditorForUnity.git?path=Assets/UnityPackages/Mmzkworks.PropertyTreeEditorForUnity
```

## Samples

PropertySystemではgetter,setterを経由することで既存コードの変数と結合することができます。

```c#
[SerializeField] private float speed = 5f;
[SerializeField] private float volume = 0.75f;

// Property一覧の定義
// NOTE: 複数プロパティに対応しています。
// NOTE: PropertyGroupによるグルーピングに対応しています。
// NOTE: bool, int, float, string, enumに対応しています。
var props = new IProperty[]
{
    new FloatPropertyAdapter("Speed", 0f, 10f, () => speed, v => speed = v),
    new FloatPropertyAdapter("Volume", 0f, 1f, () => volume, v => volume = v),
    // NOTE: PropertyInspectorではActionにButtonが対応しています。
    new ActionProperty("Run", action: () => Debug.Log("Run")),
    new PropertyGroup("GroupA", new IProperty[]
    {
        // NOTE: Adapterクラスでgetter, setterを経由すれことで既存変数の編集に使えます。
        new FloatPropertyAdapter("Speed", 0f, 10f, () => speed, v => speed = v),
        new FloatPropertyAdapter("Volume", 0f, 1f, () => volume, v => volume = v),
        new ActionProperty("Run", action: () => Debug.Log("Run"))
    }),
    new PropertyGroup("GroupB", new IProperty[]
    {
        new ActionProperty("Run", action: () =>
        {
            var property = _propertyGroup.At<BoolProperty>(0);
            property.Set(!property.Get());
        }),
        new ActionProperty("Run", action: () =>
        {
            var property = _propertyGroup.At<BoolProperty>(1);
            property.Set(!property.Get());
        })
    }),
    new PropertyGroup("Enums", new IProperty[]
    {
        new EnumProperty<PlayerState>("Player"),
        new EnumProperty<WeaponType>("Weapon")
    })
};

// UIPropertyInspectorにBindすることで全プロパティが表示されます。
var inspector = _uiDocument.rootVisualElement.Q<UIPropertyInspector>();
inspector.BindProperties(props);
inspector.BindPropertyGroup(new PropertyGroup("root", props));
```



```c#
// プロパティ木の作成
var rootGroup = new PropertyGroup("RootGroup", new[] {
	new PropertyGroup("SubGroup1", new[] {
		new FloatPropertyAdapter("FloatValue1", 0f, 100f, () => 50f, v => Debug.Log($"Set: {v}"));
    })
});

// path文字列でクエリできます
var floatProp1 = rootGroup.FindByPath("RootGroup.SubGroup1.FloatValue1");

// path文字列を取得できます : "RootGroup.SubGroup1.FloatValue1"
var path = floatProp1.GetFullPath();
```

