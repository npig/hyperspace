namespace Hyperspace 
{
	public class ReturnToFrontEndEvent : Event
	{
		// Store an object that tells your frontend where to go upon arriving.
		// This may be things like what course data to go directly back to, etc.
		public object ReturnToFrontEndData;

		public ReturnToFrontEndEvent Init(object arrivalData)
		{
			ReturnToFrontEndData = arrivalData;
			return this;
		}
	}	
}