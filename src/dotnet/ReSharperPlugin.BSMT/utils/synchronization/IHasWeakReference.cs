using System;

namespace ReSharperPlugin.BSMT_Rider.utils
{
	public interface IHasWeakReference
	{
		WeakReference WeakReference { get; }
	}
}