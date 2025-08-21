using System.ComponentModel;

namespace JsonSurfer.Core.Models;

public class JsonNode : INotifyPropertyChanged
{
    private string _key = string.Empty;
    private object? _value;
    private JsonNodeType _type;

    public string Key 
    { 
        get => _key; 
        set 
        { 
            if (_key != value) 
            { 
                _key = value; 
                OnPropertyChanged(nameof(Key)); 
            } 
        } 
    }

    public object? Value 
    { 
        get => _value; 
        set 
        { 
            if (_value != value) 
            { 
                _value = value; 
                OnPropertyChanged(nameof(Value)); 
            } 
        } 
    }

    public JsonNodeType Type 
    { 
        get => _type; 
        set 
        { 
            if (_type != value) 
            { 
                _type = value; 
                OnPropertyChanged(nameof(Type)); 
            } 
        } 
    }

    public List<JsonNode> Children { get; set; } = [];
    public JsonNode? Parent { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public enum JsonNodeType
{
    Object,
    Array,
    Property,
    String,
    Number,
    Boolean,
    Null
}