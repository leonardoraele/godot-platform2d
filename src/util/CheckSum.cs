using System;

namespace Raele.Platform2D;

public class CheckSumHelper
{
	public Func<float> Calculate { get; init; }
	private float LastCheckSum = float.NaN;

	public CheckSumHelper(Func<float> getCheckSum) => this.Calculate = getCheckSum;

	public bool CheckForChanges()
	{
		float current = this.Calculate();
		if (current != this.LastCheckSum)
		{
			this.LastCheckSum = current;
			return true;
		}
		return false;
	}

	public static implicit operator float(CheckSumHelper helper) => helper.Calculate();
}
