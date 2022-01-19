namespace CreditCardApplications
{
    public class FraudLookup
    {
        // to use partial mocks , it needs to be virtual
        public bool IsFraudRisk(CreditCardApplication application)
        {
            return CheckApplication(application);            
        }

        protected virtual bool CheckApplication(CreditCardApplication application)
        {
            if (application.LastName == "Smith")
            {
                return true;
            }

            return false;
        }
    }
}
