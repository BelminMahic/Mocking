using System;

namespace CreditCardApplications
{
    public class CreditCardApplicationEvaluator
    {
        private const int AutoReferralMaxAge = 20;
        private const int HighIncomeThreshold = 100_000;
        private const int LowIncomeThreshold = 20_000;

        private readonly IFrequentFlyerNumberValidator _validator;
        private readonly FraudLookup _fraudLookup;

        public int ValidatorLookupCount { get; private set; }

        public CreditCardApplicationEvaluator(IFrequentFlyerNumberValidator validator, FraudLookup fraudLookup = null)
        {
            _validator = validator ?? throw new System.ArgumentNullException(nameof(validator));
            _validator.ValidatorLookupPerformed += ValidatorLookupPerfomed;
            _fraudLookup = fraudLookup;
        }

        private void ValidatorLookupPerfomed(object sender, EventArgs e)
        {
            ValidatorLookupCount++;
        }

        public CreditCardApplicationDecision Evaluate(CreditCardApplication application)
        {
            if(_fraudLookup != null && _fraudLookup.IsFraudRisk(application))
            {
                return CreditCardApplicationDecision.ReferredToHumanFraudRisk;
            }

            if (application.GrossAnnualIncome >= HighIncomeThreshold)
            {
                return CreditCardApplicationDecision.AutoAccepted;
            }

            if(_validator.ServiceInformation.Licence.LicenceKey == "EXPIRED") // this one is needed with having auto hierarchy
            {
                return CreditCardApplicationDecision.ReferredToHuman;
            }

            //var isValidFrequentFlyerNumber = _validator.IsValid(application.FrequentFlyerNumber);
            _validator.ValidationMode = application.Age >= 30 ? ValidationMode.Detailed : ValidationMode.Quick;

            bool isValidFrequentFlyerNumber;

            try
            {
                isValidFrequentFlyerNumber = _validator.IsValid(application.FrequentFlyerNumber);
            }
            catch (Exception)
            {
                return CreditCardApplicationDecision.ReferredToHuman;
            }

            if (!isValidFrequentFlyerNumber)
            {
                return CreditCardApplicationDecision.ReferredToHuman;
            }

            if (application.Age <= AutoReferralMaxAge)
            {
                return CreditCardApplicationDecision.ReferredToHuman;
            }

            if (application.GrossAnnualIncome < LowIncomeThreshold)
            {
                return CreditCardApplicationDecision.AutoDeclined;
            }

            return CreditCardApplicationDecision.ReferredToHuman;
        }       

        public CreditCardApplicationDecision EvaluateUsingOut(CreditCardApplication application)
        {
            if(application.GrossAnnualIncome >= HighIncomeThreshold)
            {
                return CreditCardApplicationDecision.AutoAccepted;
            }

            _validator.IsValid(application.FrequentFlyerNumber, out var isValidFrequentFlyerNumber); // we need to mock out var

            if(!isValidFrequentFlyerNumber)
            {
                return CreditCardApplicationDecision.ReferredToHuman;
            }

            if(application.Age <= AutoReferralMaxAge)
            {
                return CreditCardApplicationDecision.ReferredToHuman;
            }

            if (application.GrossAnnualIncome < LowIncomeThreshold)
            {
                return CreditCardApplicationDecision.AutoDeclined;
            }

            return CreditCardApplicationDecision.ReferredToHuman;
        }

    }
}
