using Transit.Core.Declarations;
using Transit.Core.Entities;
using Transit.Core.Templates;
using Xunit;

namespace Transit.Tests.Templates;

public class TemplateMappingTests
{
    private static DeclarationDraftRow TravelRow() => new(
        Date: new DateOnly(2026, 7, 1),
        WeekdayName: "Wednesday",
        WorkLocationId: 2,
        LocationName: "Office",
        ColorHex: "#3B5BDB",
        CountsAsTravel: true,
        IsUnmarkedDefault: false,
        SelectedRouteId: 10,
        RouteName: "Home → Office",
        OutboundKm: 11.6m,
        ReturnKm: 10.2m,
        Kilometers: 21.8m,
        HasNoRoute: false,
        FromLocationName: "Home",
        FromPostcode: "1234 AB",
        ToLocationName: "Office",
        ToPostcode: "5678 CD");

    [Theory]
    [InlineData(TemplateColumnField.WeekdayName, "Wednesday")]
    [InlineData(TemplateColumnField.FromLocation, "Home")]
    [InlineData(TemplateColumnField.ToLocation, "Office")]
    [InlineData(TemplateColumnField.FromPostcode, "1234 AB")]
    [InlineData(TemplateColumnField.ToPostcode, "5678 CD")]
    [InlineData(TemplateColumnField.RouteName, "Home → Office")]
    public void ResolveColumnValue_returns_string_fields(TemplateColumnField field, string expected)
        => Assert.Equal(expected, TemplateMapping.ResolveColumnValue(TravelRow(), field));

    [Fact]
    public void ResolveColumnValue_date_is_a_DateOnly()
        => Assert.Equal(new DateOnly(2026, 7, 1), TemplateMapping.ResolveColumnValue(TravelRow(), TemplateColumnField.Date));

    [Fact]
    public void ResolveColumnValue_kilometers_is_a_decimal_not_a_string()
    {
        var value = TemplateMapping.ResolveColumnValue(TravelRow(), TemplateColumnField.Kilometers);
        Assert.Equal(21.8m, Assert.IsType<decimal>(value));
    }

    [Fact]
    public void ResolveColumnValue_legs_resolve_separately()
    {
        Assert.Equal(11.6m, TemplateMapping.ResolveColumnValue(TravelRow(), TemplateColumnField.OutboundKm));
        Assert.Equal(10.2m, TemplateMapping.ResolveColumnValue(TravelRow(), TemplateColumnField.ReturnKm));
    }

    [Fact]
    public void ResolveCellValue_month_name_is_dutch()
    {
        var cell = new CellMapping { CellRef = "G5", Source = TemplateCellSource.MonthName };
        Assert.Equal("juli", TemplateMapping.ResolveCellValue(cell, 2026, 7));
    }

    [Fact]
    public void ResolveCellValue_year_is_the_int()
    {
        var cell = new CellMapping { CellRef = "H1", Source = TemplateCellSource.Year };
        Assert.Equal(2026, TemplateMapping.ResolveCellValue(cell, 2026, 7));
    }

    [Fact]
    public void ResolveCellValue_static_sources_return_their_literal()
    {
        var name = new CellMapping { CellRef = "C5", Source = TemplateCellSource.EmployeeName, Value = "Rens van Eck" };
        Assert.Equal("Rens van Eck", TemplateMapping.ResolveCellValue(name, 2026, 7));
    }

    [Fact]
    public void ResolveCellValue_blank_static_source_is_null()
    {
        var empty = new CellMapping { CellRef = "C5", Source = TemplateCellSource.Custom, Value = "  " };
        Assert.Null(TemplateMapping.ResolveCellValue(empty, 2026, 7));
    }

    [Theory]
    [InlineData(10, 15, 0)]   // under capacity — no inserts
    [InlineData(15, 15, 0)]   // exactly full — no inserts
    [InlineData(18, 15, 3)]   // three over — insert three
    public void RowsToInsert_counts_the_overflow(int travelRows, int dataRowCount, int expected)
        => Assert.Equal(expected, TemplateMapping.RowsToInsert(travelRows, dataRowCount));
}