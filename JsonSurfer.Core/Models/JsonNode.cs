using System.ComponentModel;

namespace JsonSurfer.Core.Models;

public class JsonNode : INotifyPropertyChanged
{
    private string _key = string.Empty;
    private object? _value;
    private JsonNodeType _type;
    private List<JsonNode> _children = [];
    private JsonNode? _parent;
    private int _line;
    private int _column;
    private bool _isExpanded = false; // Default to collapsed for better performance

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

    public List<JsonNode> Children 
    { 
        get => _children; 
        set 
        { 
            if (_children != value) 
            { 
                _children = value; 
                OnPropertyChanged(nameof(Children)); 
            } 
        } 
    }

    public JsonNode? Parent 
    { 
        get => _parent; 
        set 
        { 
            if (_parent != value) 
            { 
                _parent = value; 
                OnPropertyChanged(nameof(Parent)); 
            } 
        } 
    }

    public int Line 
    { 
        get => _line; 
        set 
        { 
            if (_line != value) 
            { 
                _line = value; 
                OnPropertyChanged(nameof(Line)); 
            } 
        } 
    }

    public int Column 
    { 
        get => _column; 
        set 
        { 
            if (_column != value) 
            { 
                _column = value; 
                OnPropertyChanged(nameof(Column)); 
            } 
        } 
    }

    public bool IsExpanded 
    { 
        get => _isExpanded; 
        set 
        { 
            if (_isExpanded != value) 
            { 
                _isExpanded = value; 
                OnPropertyChanged(nameof(IsExpanded)); 
            } 
        } 
    }

    // Helper property to get unique path for state tracking
    public string NodePath
    {
        get
        {
            var path = new List<string>();
            var current = this;
            while (current?.Parent != null)
            {
                path.Insert(0, current.Key);
                current = current.Parent;
            }
            return string.Join(".", path);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    // Helper methods for expand/collapse operations
    public void ExpandAll()
    {
        // Set expanded state for all nodes, regardless of having children
        // This ensures consistent state for both parent and leaf nodes
        IsExpanded = true;
        foreach (var child in Children)
        {
            child.ExpandAll();
        }
    }

    public void CollapseAll()
    {
        // Set collapsed state for all nodes, regardless of having children
        // This ensures consistent state for both parent and leaf nodes
        IsExpanded = false;
        foreach (var child in Children)
        {
            child.CollapseAll();
        }
    }

    public override string ToString()
    {
        var typeStr = Type switch
        {
            JsonNodeType.Object => "[object]",
            JsonNodeType.Array => "[array]",
            JsonNodeType.String => "[string]",
            JsonNodeType.Number => "[number]",
            JsonNodeType.Boolean => "[boolean]",
            JsonNodeType.Null => "[null]",
            JsonNodeType.Property => "",
            _ => ""
        };

        if (string.IsNullOrEmpty(Key))
        {
            return $"{Value} {typeStr}".Trim();
        }
        
        if (Value == null)
        {
            return $"{Key} {typeStr}".Trim();
        }

        return $"{Key}: {Value} {typeStr}".Trim();
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