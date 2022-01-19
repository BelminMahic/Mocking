using System;

namespace CreditCardApplications
{
    public interface ILicenceData
    {
        string LicenceKey { get; }

    }
    public interface IServiceInformation
    {
        ILicenceData Licence { get; }
    }
    public interface IFrequentFlyerNumberValidator
    {
        bool IsValid(string frequentFlyerNumber);
        void IsValid(string frequentFlyerNumber, out bool isValid);
        IServiceInformation ServiceInformation { get; }
        ValidationMode ValidationMode { get; set; }
        event EventHandler ValidatorLookupPerformed;

    }
}