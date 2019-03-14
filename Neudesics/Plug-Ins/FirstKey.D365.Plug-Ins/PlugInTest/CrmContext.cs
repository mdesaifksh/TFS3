using Microsoft.Xrm.Sdk;
using System;

namespace PlugInTest
{

    public class NullCrmTracingService : ITracingService
    {
        public void Trace(string format, params object[] args)
        {
            //do nothing
        }
    }
    public class CrmContext : IPluginExecutionContext
    {
        public Guid BusinessUnitId
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Guid CorrelationId
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int Depth
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Guid InitiatingUserId
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        private ParameterCollection inputParameters;
        public ParameterCollection InputParameters
        {
            get
            {
                if (inputParameters == null)
                {
                    inputParameters = new ParameterCollection();
                }
                return inputParameters;
            }
            set
            {
                inputParameters = value;
            }

        }

        public bool IsExecutingOffline
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsInTransaction
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsOfflinePlayback
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int IsolationMode
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string MessageName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int Mode
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public DateTime OperationCreatedOn
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Guid OperationId
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Guid OrganizationId
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string OrganizationName
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        private ParameterCollection outputParameters;
        public ParameterCollection OutputParameters
        {
            get
            {
                if (outputParameters == null)
                {
                    outputParameters = new ParameterCollection();
                }
                return outputParameters;
            }
            set
            {
                outputParameters = value;
            }
        }

        public EntityReference OwningExtension
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IPluginExecutionContext ParentContext
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public EntityImageCollection PostEntityImages
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public EntityImageCollection PreEntityImages
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Guid PrimaryEntityId
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string PrimaryEntityName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Guid? RequestId
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string SecondaryEntityName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ParameterCollection SharedVariables
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int Stage
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Guid UserId
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }

}
