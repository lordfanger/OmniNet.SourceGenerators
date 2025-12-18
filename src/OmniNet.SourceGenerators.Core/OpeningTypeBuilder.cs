namespace OmniNet.SourceGenerators.Core;

/// <summary>
/// Builder to generate type name with modifiers.
/// </summary>
public ref struct OpeningTypeBuilder
{
    private readonly StringBuilderWrapper _sbWrapper;
    private readonly string _name;
    private readonly string _type;
    private Accessibility _accessibility = Accessibility.NotApplicable;
    private bool _isPartial;

    /// <summary><inheritdoc cref="OpeningTypeBuilder" path="/summary"/></summary>
    /// <param name="sbWrapper"><inheritdoc cref="StringBuilderWrapper" path="/summary"/></param>
    /// <param name="name">Type name.</param>
    /// <param name="typeKind">Kind of type.</param>
    internal OpeningTypeBuilder(StringBuilderWrapper sbWrapper, string name, GeneratedTypeKind typeKind)
    {
        _sbWrapper = sbWrapper;
        _name = name;
        _type = typeKind switch
        {
            GeneratedTypeKind.Class => "class",
            GeneratedTypeKind.Interface => "interface",
            GeneratedTypeKind.Struct => "struct",
            GeneratedTypeKind.Record => "record",
            GeneratedTypeKind.RecordStruct => "record struct",
            _ => throw new ArgumentOutOfRangeException(nameof(typeKind), typeKind, null)
        };
    }

    /// <summary>
    /// Sets type's partial modifier.
    /// <code>partial</code>
    /// </summary>
    /// <param name="value">Whether partial modifier should be set.</param>
    /// <returns>Self builder.</returns>
    public OpeningTypeBuilder WithPartial(bool value = true)
    {
        _isPartial = value;
        return this;
    }

    /// <summary>
    /// Set's type's accessibility.
    /// </summary>
    /// <param name="accessibility">Desired type's accessibility.</param>
    /// <returns>Self builder.</returns>
    public OpeningTypeBuilder WithAccessibility(Accessibility accessibility)
    {
        _accessibility = accessibility;
        return this;
    }

    /// <summary>
    /// Appends type to source builder.
    /// </summary>
    /// <returns>Builder to conditionally set type's inheritance.</returns>
    public readonly TypeInheritanceBuilder Append()
    {
        if (_accessibility != Accessibility.NotApplicable)
        {
            _sbWrapper.Append(SyntaxFacts.GetText(_accessibility)).Append(' ');
        }

        if (_isPartial)
        {
            _sbWrapper.Append("partial ");
        }

        _sbWrapper.Append(_type).Append(' ').Append(_name);
        return new TypeInheritanceBuilder(_sbWrapper);
    }
}