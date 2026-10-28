interface IAttributionOptions
{
    static abstract bool Timestamp { get; }
    static abstract bool Lock { get; }
    static abstract bool Guards { get; }
    static abstract bool Priority { get; }
    static abstract bool Reference { get; }
}

readonly struct AttributionOptions0 : IAttributionOptions
{
    public static bool Timestamp => true;
    public static bool Lock => true;
    public static bool Guards => true;
    public static bool Priority => true;
    public static bool Reference => false;
}

readonly struct AttributionOptions1 : IAttributionOptions
{
    public static bool Timestamp => false;
    public static bool Lock => true;
    public static bool Guards => true;
    public static bool Priority => true;
    public static bool Reference => false;
}

readonly struct AttributionOptions2 : IAttributionOptions
{
    public static bool Timestamp => true;
    public static bool Lock => false;
    public static bool Guards => true;
    public static bool Priority => true;
    public static bool Reference => false;
}

readonly struct AttributionOptions3 : IAttributionOptions
{
    public static bool Timestamp => false;
    public static bool Lock => false;
    public static bool Guards => true;
    public static bool Priority => true;
    public static bool Reference => false;
}

readonly struct AttributionOptions4 : IAttributionOptions
{
    public static bool Timestamp => true;
    public static bool Lock => true;
    public static bool Guards => false;
    public static bool Priority => true;
    public static bool Reference => false;
}

readonly struct AttributionOptions5 : IAttributionOptions
{
    public static bool Timestamp => false;
    public static bool Lock => true;
    public static bool Guards => false;
    public static bool Priority => true;
    public static bool Reference => false;
}

readonly struct AttributionOptions6 : IAttributionOptions
{
    public static bool Timestamp => true;
    public static bool Lock => false;
    public static bool Guards => false;
    public static bool Priority => true;
    public static bool Reference => false;
}

readonly struct AttributionOptions7 : IAttributionOptions
{
    public static bool Timestamp => false;
    public static bool Lock => false;
    public static bool Guards => false;
    public static bool Priority => true;
    public static bool Reference => false;
}

readonly struct AttributionOptions8 : IAttributionOptions
{
    public static bool Timestamp => true;
    public static bool Lock => true;
    public static bool Guards => true;
    public static bool Priority => false;
    public static bool Reference => false;
}

readonly struct AttributionOptions9 : IAttributionOptions
{
    public static bool Timestamp => false;
    public static bool Lock => true;
    public static bool Guards => true;
    public static bool Priority => false;
    public static bool Reference => false;
}

readonly struct AttributionOptions10 : IAttributionOptions
{
    public static bool Timestamp => true;
    public static bool Lock => false;
    public static bool Guards => true;
    public static bool Priority => false;
    public static bool Reference => false;
}

readonly struct AttributionOptions11 : IAttributionOptions
{
    public static bool Timestamp => false;
    public static bool Lock => false;
    public static bool Guards => true;
    public static bool Priority => false;
    public static bool Reference => false;
}

readonly struct AttributionOptions12 : IAttributionOptions
{
    public static bool Timestamp => true;
    public static bool Lock => true;
    public static bool Guards => false;
    public static bool Priority => false;
    public static bool Reference => false;
}

readonly struct AttributionOptions13 : IAttributionOptions
{
    public static bool Timestamp => false;
    public static bool Lock => true;
    public static bool Guards => false;
    public static bool Priority => false;
    public static bool Reference => false;
}

readonly struct AttributionOptions14 : IAttributionOptions
{
    public static bool Timestamp => true;
    public static bool Lock => false;
    public static bool Guards => false;
    public static bool Priority => false;
    public static bool Reference => false;
}

readonly struct AttributionOptions15 : IAttributionOptions
{
    public static bool Timestamp => false;
    public static bool Lock => false;
    public static bool Guards => false;
    public static bool Priority => false;
    public static bool Reference => false;
}

readonly struct AttributionOptions16 : IAttributionOptions
{
    public static bool Timestamp => true;
    public static bool Lock => true;
    public static bool Guards => true;
    public static bool Priority => true;
    public static bool Reference => true;
}

readonly struct AttributionOptions17 : IAttributionOptions
{
    public static bool Timestamp => false;
    public static bool Lock => true;
    public static bool Guards => true;
    public static bool Priority => true;
    public static bool Reference => true;
}

readonly struct AttributionOptions18 : IAttributionOptions
{
    public static bool Timestamp => true;
    public static bool Lock => false;
    public static bool Guards => true;
    public static bool Priority => true;
    public static bool Reference => true;
}

readonly struct AttributionOptions19 : IAttributionOptions
{
    public static bool Timestamp => false;
    public static bool Lock => false;
    public static bool Guards => true;
    public static bool Priority => true;
    public static bool Reference => true;
}

readonly struct AttributionOptions20 : IAttributionOptions
{
    public static bool Timestamp => true;
    public static bool Lock => true;
    public static bool Guards => false;
    public static bool Priority => true;
    public static bool Reference => true;
}

readonly struct AttributionOptions21 : IAttributionOptions
{
    public static bool Timestamp => false;
    public static bool Lock => true;
    public static bool Guards => false;
    public static bool Priority => true;
    public static bool Reference => true;
}

readonly struct AttributionOptions22 : IAttributionOptions
{
    public static bool Timestamp => true;
    public static bool Lock => false;
    public static bool Guards => false;
    public static bool Priority => true;
    public static bool Reference => true;
}

readonly struct AttributionOptions23 : IAttributionOptions
{
    public static bool Timestamp => false;
    public static bool Lock => false;
    public static bool Guards => false;
    public static bool Priority => true;
    public static bool Reference => true;
}

readonly struct AttributionOptions24 : IAttributionOptions
{
    public static bool Timestamp => true;
    public static bool Lock => true;
    public static bool Guards => true;
    public static bool Priority => false;
    public static bool Reference => true;
}

readonly struct AttributionOptions25 : IAttributionOptions
{
    public static bool Timestamp => false;
    public static bool Lock => true;
    public static bool Guards => true;
    public static bool Priority => false;
    public static bool Reference => true;
}

readonly struct AttributionOptions26 : IAttributionOptions
{
    public static bool Timestamp => true;
    public static bool Lock => false;
    public static bool Guards => true;
    public static bool Priority => false;
    public static bool Reference => true;
}

readonly struct AttributionOptions27 : IAttributionOptions
{
    public static bool Timestamp => false;
    public static bool Lock => false;
    public static bool Guards => true;
    public static bool Priority => false;
    public static bool Reference => true;
}

readonly struct AttributionOptions28 : IAttributionOptions
{
    public static bool Timestamp => true;
    public static bool Lock => true;
    public static bool Guards => false;
    public static bool Priority => false;
    public static bool Reference => true;
}

readonly struct AttributionOptions29 : IAttributionOptions
{
    public static bool Timestamp => false;
    public static bool Lock => true;
    public static bool Guards => false;
    public static bool Priority => false;
    public static bool Reference => true;
}

readonly struct AttributionOptions30 : IAttributionOptions
{
    public static bool Timestamp => true;
    public static bool Lock => false;
    public static bool Guards => false;
    public static bool Priority => false;
    public static bool Reference => true;
}

readonly struct AttributionOptions31 : IAttributionOptions
{
    public static bool Timestamp => false;
    public static bool Lock => false;
    public static bool Guards => false;
    public static bool Priority => false;
    public static bool Reference => true;
}

