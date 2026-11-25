namespace Aircraft.Core.Code;

public class ReproductionAttribute : Attribute {
    public ReproductionAttribute(string paper, int year) { }
    public ReproductionAttribute() { }
}

/// 用于Debug测试的flag,硬编码在逻辑中，用于测试
public class DebugConfigAttribute : Attribute { }