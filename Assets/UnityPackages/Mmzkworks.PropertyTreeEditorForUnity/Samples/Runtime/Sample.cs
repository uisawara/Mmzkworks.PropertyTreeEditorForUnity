using System;
using UnityEngine;
using UnityEngine.UIElements;
using works.mmzk.PropertySystem;
using works.mmzk.PropertySystem.Utilities;
using works.mmzk.PropertyTree;

public class Sample : MonoBehaviour
{
    [SerializeField] private UIDocument _uiDocument;

    [SerializeField] private float speed = 5f;
    [SerializeField] private float volume = 0.75f;

    public SampleA sampleA = new();
    
    private PropertyGroup _propertyGroup = new PropertyGroup("toAnimator", items: new[]
    {
        new BoolProperty("Jump"),
        new BoolProperty("Rest")
    });
    
    public enum PlayerState
    {
        Idle,
        Walking,
        Running,
        Jumping,
        Falling
    }

    public enum WeaponType
    {
        Sword,
        Bow,
        Staff,
        Dagger
    }

    void Start()
    {
        var serializablePropertyGroup = SerializablePropertyBuilder.BuildFromObject(sampleA);
        var rootGroup = new PropertyGroup("root", new IProperty[]
        {
            new FloatPropertyAdapter("Speed", 0f, 10f, () => speed, v => speed = v),
            new FloatPropertyAdapter("Volume", 0f, 1f, () => volume, v => volume = v),
            new ActionProperty("Run", action: () => Debug.Log("Run")),
            new PropertyGroup("GroupA", new IProperty[]
            {
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
            }),
            serializablePropertyGroup
        });

        var inspector = _uiDocument.rootVisualElement.Q<UIPropertyInspector>();
        inspector.BindPropertyGroup(rootGroup);
    }
}

[Serializable]
public class SampleA
{
    public int i;
    public float f;
    public string name;
}
