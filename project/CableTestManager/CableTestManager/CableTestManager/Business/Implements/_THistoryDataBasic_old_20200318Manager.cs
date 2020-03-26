using System;
using System.Collections.Generic;
using System.Text;

namespace CableTestManager.Business.Implements
{
    using CableTestManager.Entity;
    using CableTestManager.Data.Interface;
    using CableTestManager.Data.Implements;
    using CableTestManager.Business.Interface;
    public class _THistoryDataBasic_old_20200318Manager : BaseManager<_THistoryDataBasic_old_20200318>, I_THistoryDataBasic_old_20200318Manager
    {
		#region ����ע��
		
        private I_THistoryDataBasic_old_20200318Service service = new _THistoryDataBasic_old_20200318Service();

        public _THistoryDataBasic_old_20200318Manager()
        {
            base.BaseService = this.service;
        }
        
        #endregion
    }
}