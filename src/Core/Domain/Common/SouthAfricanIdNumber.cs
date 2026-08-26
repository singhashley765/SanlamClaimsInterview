namespace SanlamClaims.Domain.Common;

/// <summary>Validates a South African 13-digit ID number: date of birth, citizenship digit, and a Luhn check digit.</summary>
public static class SouthAfricanIdNumber
{
    public static bool IsValid(string? idNumber)
    {
        if (idNumber is not { Length: 13 } || !idNumber.All(char.IsAsciiDigit))
        {
            return false;
        }

        if (!HasValidDateOfBirth(idNumber))
        {
            return false;
        }

        var citizenship = idNumber[10];
        if (citizenship is not ('0' or '1'))
        {
            return false;
        }

        return HasValidCheckDigit(idNumber);
    }

    private static bool HasValidDateOfBirth(string idNumber)
    {
        var month = ((idNumber[2] - '0') * 10) + (idNumber[3] - '0');
        var day = ((idNumber[4] - '0') * 10) + (idNumber[5] - '0');

        if (month is < 1 or > 12)
        {
            return false;
        }

        var yy = ((idNumber[0] - '0') * 10) + (idNumber[1] - '0');

        return day >= 1 && (day <= DateTime.DaysInMonth(1900 + yy, month) || day <= DateTime.DaysInMonth(2000 + yy, month));
    }

    private static bool HasValidCheckDigit(string idNumber)
    {
        var oddDigitSum = 0;
        for (var i = 0; i < 12; i += 2)
        {
            oddDigitSum += idNumber[i] - '0';
        }

        var evenDigitsAsNumber = long.Parse(string.Concat(idNumber[1], idNumber[3], idNumber[5], idNumber[7], idNumber[9], idNumber[11]));
        var doubled = (evenDigitsAsNumber * 2).ToString();
        var evenDigitSum = doubled.Sum(c => c - '0');

        var checkDigit = (10 - ((oddDigitSum + evenDigitSum) % 10)) % 10;
        return checkDigit == idNumber[12] - '0';
    }
}
