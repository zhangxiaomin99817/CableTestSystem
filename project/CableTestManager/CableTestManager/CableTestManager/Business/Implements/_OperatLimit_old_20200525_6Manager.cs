using System;
using System.Collections.Generic;
using System.Text;

namespace CableTestManager.Business.Implements
{
    using CableTestManager.Entity;
    using CableTestManager.Data.Interface;
    using CableTestManager.Data.Implements;
    using CableTestManager.Business.Interface;
    public class _OperatLimit_old_20200525_6Manager : BaseManager<_OperatLimit_old_20200525_6>, I_OperatLimit_old_20200525_6Manager
    {
		#region ����ע��
		
        private I_OperatLimit_old_20200525_6DBService dBService = new _OperatLimit_old_20200525_6DBService();

        public _OperatLimit_old_20200525_6Manager()
        {
            base.BaseDBService = this.dBService;
        }
        
        #endregion
    }
}