using System;
using System.Collections.Generic;
using System.Text;

namespace CableTestManager.Business.Implements
{
    using CableTestManager.Entity;
    using CableTestManager.Data.Interface;
    using CableTestManager.Data.Implements;
    using CableTestManager.Business.Interface;
    public class _InterfaceInfoLibrary_old_20200618Manager : BaseManager<_InterfaceInfoLibrary_old_20200618>, I_InterfaceInfoLibrary_old_20200618Manager
    {
		#region ����ע��
		
        private I_InterfaceInfoLibrary_old_20200618Service service = new _InterfaceInfoLibrary_old_20200618Service();

        public _InterfaceInfoLibrary_old_20200618Manager()
        {
            base.BaseService = this.service;
        }
        
        #endregion
    }
}
