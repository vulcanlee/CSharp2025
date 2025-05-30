namespace Business.Extensions;

public static class StringToNumberExtension
{
    public static double ToBloodDouble(this string value)
    {
        double result2;
        double interval = 0.1;
        string operatorValue = "";

        operatorValue = ">";
        result2 = 0;
        if (value.Contains(operatorValue))
        {
            var values = value.Split(operatorValue);
            double.TryParse(values[1], out result2);
            return result2 + interval;
        }

        operatorValue = "≧";
        result2 = 0;
        if (value.Contains(operatorValue))
        {
            var values = value.Split(operatorValue);
            double.TryParse(values[1], out result2);
            return result2 + interval;
        }

        operatorValue = "<";
        result2 = 0;
        if (value.Contains(operatorValue))
        {
            var values = value.Split(operatorValue);
            double.TryParse(values[1], out result2);
            return result2 - interval;
        }

        operatorValue = "≦";
        result2 = 0;
        if (value.Contains(operatorValue))
        {
            var values = value.Split(operatorValue);
            double.TryParse(values[1], out result2);
            return result2 - interval;
        }

        return double.TryParse(value, out double result) ? result : 0;
    }
    public static double ToDouble(this string value)
    {
        return double.TryParse(value, out double result) ? result : 0;
    }
    public static float ToFloat(this string value)
    {
        return float.TryParse(value, out float result) ? result : 0;
    }

    public static int ToInt(this string value)
    {
        return int.TryParse(value, out int result) ? result : 0;
    }
}
