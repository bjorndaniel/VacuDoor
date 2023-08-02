namespace VacuDoor;

internal static class ServoControl
{
    private static bool _isOpen;

    public static bool Open(ref ServoMotor motor)
    {
        try
        {
            if (_isOpen)
            {
                return false;
            }
            motor.WriteAngle(180);
            _isOpen = true;
            return true;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.ToString());
        }
        return false;
    }

    public static bool Close(ref ServoMotor motor)
    {
        try
        {
            if (!_isOpen)
            {
                return false;
            }
            motor.WriteAngle(0);
            _isOpen = false;
            return true;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.ToString());
        }
        return false;
    }
}
