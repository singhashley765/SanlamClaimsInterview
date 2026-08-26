using FluentAssertions;
using SanlamClaims.Domain.Common;

namespace SanlamClaims.Tests.Unit.Domain;

[TestClass]
public class SouthAfricanIdNumberTests
{
    [TestMethod]
    [DataRow("8501015800088")]
    [DataRow("9202124800080")]
    [DataRow("7710305800085")]
    public void IsValid_KnownValidIdNumbers_ReturnsTrue(string idNumber)
    {
        SouthAfricanIdNumber.IsValid(idNumber).Should().BeTrue();
    }

    [TestMethod]
    public void IsValid_WrongCheckDigit_ReturnsFalse()
    {
        SouthAfricanIdNumber.IsValid("8501015800080").Should().BeFalse();
    }

    [TestMethod]
    public void IsValid_TooShort_ReturnsFalse()
    {
        SouthAfricanIdNumber.IsValid("850101580008").Should().BeFalse();
    }

    [TestMethod]
    public void IsValid_ContainsNonDigits_ReturnsFalse()
    {
        SouthAfricanIdNumber.IsValid("85010A5800088").Should().BeFalse();
    }

    [TestMethod]
    public void IsValid_InvalidMonth_ReturnsFalse()
    {
        SouthAfricanIdNumber.IsValid("8513015800088").Should().BeFalse();
    }

    [TestMethod]
    public void IsValid_InvalidCitizenshipDigit_ReturnsFalse()
    {
        SouthAfricanIdNumber.IsValid("8501015800288").Should().BeFalse();
    }

    [TestMethod]
    public void IsValid_Null_ReturnsFalse()
    {
        SouthAfricanIdNumber.IsValid(null).Should().BeFalse();
    }
}
