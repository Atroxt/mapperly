using Riok.Mapperly.Diagnostics;

namespace Riok.Mapperly.Tests.Mapping;

[UsesVerify]
public class ObjectPropertyInitPropertyTest
{
    [Fact]
    public void InitOnlyProperty()
    {
        var source = TestSourceBuilder.Mapping(
            "A",
            "B",
            "class A { public string StringValue { get; init; } public int IntValue { get; set; } }",
            "class B { public string StringValue { get; init; } public int IntValue { get; set; } }"
        );

        TestHelper
            .GenerateMapper(source)
            .Should()
            .HaveSingleMethodBody(
                """
                var target = new global::B()
                {
                    StringValue = source.StringValue,
                };
                target.IntValue = source.IntValue;
                return target;
                """
            );
    }

    [Fact]
    public void MultipleInitOnlyProperties()
    {
        var source = TestSourceBuilder.Mapping(
            "A",
            "B",
            "class A { public string StringValue { get; init; } public int IntValue { get; set; } }",
            "class B { public string StringValue { get; init; } public int IntValue { get; init; } }"
        );

        TestHelper
            .GenerateMapper(source)
            .Should()
            .HaveSingleMethodBody(
                """
                var target = new global::B()
                {
                    StringValue = source.StringValue,
                    IntValue = source.IntValue,
                };
                return target;
                """
            );
    }

    [Fact]
    public void InitOnlyPropertyWithNullableSource()
    {
        var source = TestSourceBuilder.Mapping(
            "A",
            "B",
            "class A { public string? Value { get; init; } }",
            "class B { public string Value { get; init; } }"
        );

        TestHelper
            .GenerateMapper(source)
            .Should()
            .HaveSingleMethodBody(
                """
                var target = new global::B()
                {
                    Value = source.Value ?? throw new System.ArgumentNullException(nameof(source.Value)),
                };
                return target;
                """
            );
    }

    [Fact]
    public void InitOnlyPropertyWithNullableSourceNoThrow()
    {
        var source = TestSourceBuilder.Mapping(
            "A",
            "B",
            TestSourceBuilderOptions.Default with
            {
                ThrowOnMappingNullMismatch = false
            },
            "class A { public string? Value { get; init; } }",
            "class B { public string Value { get; init; } }"
        );

        TestHelper
            .GenerateMapper(source)
            .Should()
            .HaveSingleMethodBody(
                """
                var target = new global::B()
                {
                    Value = source.Value ?? "",
                };
                return target;
                """
            );
    }

    [Fact]
    public void InitOnlyPropertyWithConfiguration()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            "[MapProperty($\"StringValue2\", \"StringValue\")] partial B Map(A source);",
            "class A { public string StringValue2 { get; init; } }",
            "class B { public string StringValue { get; init; } }"
        );

        TestHelper
            .GenerateMapper(source)
            .Should()
            .HaveSingleMethodBody(
                """
                var target = new global::B()
                {
                    StringValue = source.StringValue2,
                };
                return target;
                """
            );
    }

    [Fact]
    public void InitOnlyReferenceLoop()
    {
        var source = TestSourceBuilder.Mapping(
            "A",
            "B",
            "class A { public A? Parent { get; init; } }",
            "class B { public B? Parent { get; init; } }"
        );

        TestHelper
            .GenerateMapper(source)
            .Should()
            .HaveSingleMethodBody(
                """
                var target = new global::B()
                {
                    Parent = source.Parent != null ? Map(source.Parent) : default,
                };
                return target;
                """
            );
    }

    [Fact]
    public void InitOnlyPropertyWithAutoFlattenedNullablePath()
    {
        var source = TestSourceBuilder.Mapping(
            "A",
            "B",
            "class A { public C? Nested { get; init; } }",
            "class B { public string NestedValue { get; init; } }",
            "class C { public string Value { get; } }"
        );

        TestHelper
            .GenerateMapper(source)
            .Should()
            .HaveSingleMethodBody(
                """
                var target = new global::B()
                {
                    NestedValue = source.Nested?.Value ?? throw new System.ArgumentNullException(nameof(source.Nested.Value)),
                };
                return target;
                """
            );
    }

    [Fact]
    public void InitOnlyPropertyWithAutoFlattened()
    {
        var source = TestSourceBuilder.Mapping(
            "A",
            "B",
            "class A { public C Nested { get; init; } }",
            "class B { public string NestedValue { get; init; } }",
            "class C { public string Value { get; } }"
        );

        TestHelper
            .GenerateMapper(source)
            .Should()
            .HaveSingleMethodBody(
                """
                var target = new global::B()
                {
                    NestedValue = source.Nested.Value,
                };
                return target;
                """
            );
    }

    [Fact]
    public void InitOnlyPropertyShouldDiagnosticOnVoidMethod()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            "partial void Map(A source, B target);",
            "class A { public string StringValue { get; } }",
            "class B { public string StringValue { get; init; } }"
        );
        TestHelper
            .GenerateMapper(source, TestHelperOptions.AllowDiagnostics)
            .Should()
            .HaveDiagnostic(
                DiagnosticDescriptors.CannotMapToInitOnlyMemberPath,
                "Cannot map from member A.StringValue of type string to init only member path B.StringValue of type string"
            )
            .HaveDiagnostic(
                DiagnosticDescriptors.SourceMemberNotMapped,
                "The member StringValue on the mapping source type A is not mapped to any member on the mapping target type B"
            )
            .HaveAssertedAllDiagnostics();
    }

    [Fact]
    public void InitOnlyPropertySourceNotFoundShouldDiagnostic()
    {
        var source = TestSourceBuilder.Mapping(
            "A",
            "B",
            "class A { public string StringValue2 { get; init; } public int IntValue { get; set; } }",
            "class B { public string StringValue { get; init; } public int IntValue { get; set; } }"
        );

        TestHelper
            .GenerateMapper(source, TestHelperOptions.AllowDiagnostics)
            .Should()
            .HaveDiagnostic(
                DiagnosticDescriptors.SourceMemberNotFound,
                "The member StringValue on the mapping target type B was not found on the mapping source type A"
            )
            .HaveDiagnostic(
                DiagnosticDescriptors.SourceMemberNotMapped,
                "The member StringValue2 on the mapping source type A is not mapped to any member on the mapping target type B"
            )
            .HaveAssertedAllDiagnostics();
    }

    [Fact]
    public Task InitOnlyPropertyWithMultipleConfigurationsShouldDiagnostic()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            "[MapProperty($\"StringValue2\", \"StringValue\")] [MapProperty($\"StringValue3\", \"StringValue\")] private partial B Map(A source);",
            "class A { public string StringValue2 { get; init; } public string StringValue3 { get; init; } }",
            "class B { public string StringValue { get; init; } }"
        );

        return TestHelper.VerifyGenerator(source);
    }

    [Fact]
    public Task InitOnlyPropertyWithPathConfigurationsShouldDiagnostic()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            "[MapProperty($\"NestedValue\", \"Nested.Value\")] private partial B Map(A source);",
            "class A { public string NestedValue { get; init; } }",
            "class B { public C Nested { get; init; } }",
            "class C { public string Value { get; init; } }"
        );

        return TestHelper.VerifyGenerator(source);
    }

    [Fact]
    public Task InitOnlyPropertyWithConfigurationNotFoundSourcePropertyShouldDiagnostic()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            "[MapProperty($\"StringValue2\", \"StringValue\")] private partial B Map(A source);",
            "class A { public string StringValue { get; init; } }",
            "class B { public string StringValue { get; init; } }"
        );

        return TestHelper.VerifyGenerator(source);
    }

    [Fact]
    public void RequiredProperty()
    {
        var source = TestSourceBuilder.Mapping(
            "A",
            "B",
            "class A { public string StringValue { get; init; } public int IntValue { get; set; } }",
            "class B { public required string StringValue { get; set; } public int IntValue { get; set; } }"
        );

        TestHelper
            .GenerateMapper(source)
            .Should()
            .HaveSingleMethodBody(
                """
                var target = new global::B()
                {
                    StringValue = source.StringValue,
                };
                target.IntValue = source.IntValue;
                return target;
                """
            );
    }

    [Fact]
    public void IgnoredTargetRequiredPropertyWithConfiguration()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            """
                [MapProperty("StringValue", "StringValue")]
                [MapperIgnoreTarget("StringValue")]
                partial B Map(A source);
                """,
            "A",
            "B",
            "class A { public string StringValue { get; init; } public int IntValue { get; set; } }",
            "class B { public required string StringValue { get; set; } public int IntValue { get; set; } }"
        );

        TestHelper
            .GenerateMapper(source, TestHelperOptions.AllowDiagnostics)
            .Should()
            .HaveSingleMethodBody(
                """
                var target = new global::B()
                {
                    StringValue = source.StringValue,
                };
                target.IntValue = source.IntValue;
                return target;
                """
            )
            .HaveDiagnostic(
                DiagnosticDescriptors.IgnoredTargetMemberExplicitlyMapped,
                "The target member StringValue on B is ignored, but is also mapped by the MapPropertyAttribute"
            )
            .HaveAssertedAllDiagnostics();
    }

    [Fact]
    public void IgnoredTargetInitPropertyWithConfiguration()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            """
                [MapProperty("StringValue", "StringValue")]
                [MapperIgnoreTarget("StringValue")]
                partial B Map(A source);
                """,
            "A",
            "B",
            "class A { public string StringValue { get; init; } public int IntValue { get; set; } }",
            "class B { public string StringValue { get; init; } public int IntValue { get; set; } }"
        );

        TestHelper
            .GenerateMapper(source, TestHelperOptions.AllowDiagnostics)
            .Should()
            .HaveSingleMethodBody(
                """
                var target = new global::B()
                {
                    StringValue = source.StringValue,
                };
                target.IntValue = source.IntValue;
                return target;
                """
            )
            .HaveDiagnostic(
                DiagnosticDescriptors.IgnoredTargetMemberExplicitlyMapped,
                "The target member StringValue on B is ignored, but is also mapped by the MapPropertyAttribute"
            )
            .HaveAssertedAllDiagnostics();
    }

    [Fact]
    public Task RequiredPropertySourceNotFoundShouldDiagnostic()
    {
        var source = TestSourceBuilder.Mapping(
            "A",
            "B",
            "class A { public string StringValue2 { get; init; } public int IntValue { get; set; } }",
            "class B { public required string StringValue { get; init; } public int IntValue { get; set; } }"
        );

        return TestHelper.VerifyGenerator(source);
    }
}
