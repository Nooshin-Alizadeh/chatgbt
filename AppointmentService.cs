using Cloud.BaseObject;
using Cloud.BaseObject.BaseBusinessObjects;
using Cloud.BaseObject.BaseBusinessObjects.Activity;
using Cloud.BaseObject.BaseBusinessObjects.Email;
using Cloud.BaseObject.BaseBusinessObjects.EmailTemplate;
using Cloud.BaseObject.BaseBusinessObjects.Enums;
using Cloud.BaseObject.BaseBusinessObjects.Task;
using Cloud.BaseObject.BaseBusinessObjects.UserObject;
using Cloud.BaseObject.CRM;
using Cloud.BaseObject.CRM.AppointmentBusinessObject;
using Cloud.BaseObject.CRM.AppointmentConfigBusinessObject;
using Cloud.BaseObject.CRM.CommunicationBusinessObject;
using Cloud.BaseObject.CRM.CompanyBusinessObject;
using Cloud.BaseObject.CRM.ContractBusinessObject;
using Cloud.BaseObject.CRM.Enums;
using Cloud.BaseObject.CRM.LeadBusinessObject;
using Cloud.BaseObject.CRM.OpportunityBusinessObject;
using Cloud.BaseObject.CRM.PersonBusinessObject;
using Cloud.BaseObject.CRM.PhoneNumberBusinessObject;
using Cloud.BaseObject.CRM.ProjectBusinessObject;
using Cloud.BaseObject.CRM.QuestionnairyBusinessObject;
using Cloud.BaseObject.CRM.TicketingBusinessObject;
using Cloud.BusinessObject.Services.AppointmentService.Commands;
using Cloud.BusinessObject.Services.TenantService;
using Cloud.Core;
using Cloud.Core.GetCalendar;
using Cloud.Core.Result;
using ICF.Client.Core;
using ICF.Client.Core.Exceptions;
using ICF.Client.Core.Logging;
using MediatR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AppointmentType = Cloud.BaseObject.CRM.Enums.AppointmentType;
using System.Data;
using Cloud.BaseObject.CRM.CrmHistoryBusinessObject;
using ICF.Client;
using Cloud.Core.Infrastructure;
using Cloud.BaseObject.CRM.SignatureBusinessObject;
using Cloud.BaseObject.CRM.NotificationManagerBusinessObject;
using Cloud.BaseObject.BaseBusinessObjects.UserNotification;
using Cloud.Job;

namespace Cloud.BusinessObject.Services.AppointmentService
{
    public class AppointmentService : BaseBusinessService<Appointment, FlatAppointment>, IAppointmentService
    {
        public IAppointmentRepository appointmentRepository { get; set; }
        private IWorkingContext workingContext;
        private readonly IdentityClientOptions _identityOptions;
        private Lazy<ITotalActivityRepository> totalActivityRepository;
        private readonly Lazy<IAppointmentContractRepository> appointmentContractRepository;
        private readonly Lazy<IAppointmentQuestionaryRepository> appointmentQuestionaryRepository;
        public Lazy<ILeadRepository> LeadRepository { get; set; }
        public Lazy<IAppointmentConfigRepository> appointmentConfigRepository { get; set; }
        public Lazy<ILeadStateRepository> LeadStateRepository { get; set; }
        public Lazy<IUserInfoRepository> UserInfoRepository { get; set; }
        public Lazy<ILeadService> LeadService { get; set; }
        public Lazy<UtilityService> UtilityService;
        public Lazy<ICompanyRepository> CompanyRepository { get; set; }
        public Lazy<IPersonRepository> PersonRepository { get; set; }
        public Lazy<IOpportunityRepository> OpportunityRepository { get; set; }
        public Lazy<ITicketRepository> TicketRepository { get; set; }
        public Lazy<IProjectRepository> ProjectRepository { get; set; }
        public Lazy<IEmailTemplateRepository> EmailTemplateRepository { get; set; }
        public Lazy<IEmailService> EmailService { get; set; }
        private Lazy<ITaskRepository> TaskRepository { get; set; }
        public Lazy<IMediator> Mediator { get; set; }
        private static ILog logger = LogProvider.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public Lazy<ISmsService> SmsService { get; set; }
        public Lazy<TenantHttpClient> TenantHttpClient { get; set; }
        public Lazy<ITotalActivityService> TotalActivityService { get; set; }
        private readonly Lazy<IPersonService> personService;
        private readonly Lazy<IContractRepository> contractRepository;
        private readonly Lazy<IQuestionaryRepository> questionaryRepository;
        private Lazy<IHistoryTrackingService> _CrmHistoryService;
        private Lazy<ISignatureRepository> _SignatureRepository;
        private Lazy<INotificationManagerRepository> notificationManagerRepository;
        private readonly string BaseAddress = ConfigurationManager.AppSettings["BaseAddress"];
        private Lazy<INotificationService> notificationService;

        private List<FlatLead> Attendies = new List<FlatLead>();
        private NotificationManager notificationManager;
        public AppointmentService(
            IAppointmentRepository appointmentRepository,
            IdentityClientOptions identityOptions,
            IWorkingContext context,
            ICF.Client.Service.ICustomersService customersService,
            Lazy<ITotalActivityRepository> totalActivityRepository,
            Lazy<IMediator> mediator,
            Lazy<IProjectRepository> projectRepository,
            Lazy<ITicketRepository> ticketRepository,
            Lazy<IEmailService> emailService,
            Lazy<ITaskRepository> _repository,
            Lazy<IAppointmentConfigRepository> appointmentConfigRepository,
            Lazy<ILeadStateRepository> _leadStateRepository,
            Lazy<ILeadRepository> leadRepository,
            Lazy<IUserInfoRepository> userInfoRepository,
            Lazy<UtilityService> utilityService,
            Lazy<ILeadService> leadService,
            Lazy<ISmsService> smsService,
            Lazy<IPersonRepository> personRepository,
            Lazy<ICompanyRepository> companyRepository,
            Lazy<IOpportunityRepository> opportunityRepository,
            Lazy<IEmailTemplateRepository> emailTemplateRepository,
            Lazy<TenantHttpClient> tenantHttpClient,
            Lazy<ITotalActivityService> totalActivityService,
            Lazy<IContractRepository> contractRepository,
            Lazy<IQuestionaryRepository> questionaryRepository,
            Lazy<IAppointmentContractRepository> appointmentContractRepository,
            Lazy<IAppointmentQuestionaryRepository> appointmentQuestionaryRepository,
            Lazy<IPersonService> personService,
            Lazy<IHistoryTrackingService> crmHistoryService,
            Lazy<INotificationManagerRepository> _notificationManagerRepository,
            Lazy<INotificationService> _notificationService,
            Lazy<ISignatureRepository> signatureRepository) :
            base(appointmentRepository, customersService)
        {
            TotalActivityService = totalActivityService;
            TenantHttpClient = tenantHttpClient;
            TaskRepository = _repository;
            this.totalActivityRepository = totalActivityRepository;
            this.appointmentRepository = appointmentRepository;
            this.appointmentConfigRepository = appointmentConfigRepository;
            this._identityOptions = identityOptions;
            LeadRepository = leadRepository;
            LeadStateRepository = _leadStateRepository;
            UtilityService = utilityService;
            UserInfoRepository = userInfoRepository;
            LeadService = leadService;
            workingContext = context;
            CompanyRepository = companyRepository;
            PersonRepository = personRepository;
            OpportunityRepository = opportunityRepository;
            EmailTemplateRepository = emailTemplateRepository;
            EmailService = emailService;
            TicketRepository = ticketRepository;
            ProjectRepository = projectRepository;
            Mediator = mediator;
            SmsService = smsService;
            this.contractRepository = contractRepository;
            this.questionaryRepository = questionaryRepository;
            this.appointmentContractRepository = appointmentContractRepository;
            this.appointmentQuestionaryRepository = appointmentQuestionaryRepository;
            this.personService = personService;
            _CrmHistoryService = crmHistoryService;
            _SignatureRepository = signatureRepository;
            notificationManagerRepository = _notificationManagerRepository;
            notificationService = _notificationService;
        }

        public override Result Delete(Guid id)
        {
            //  ICalendarService CalendarService = new GoogleCalendar();
            try
            {
                var appointment = appointmentRepository.Get(id);
                if (appointment == null)
                    return Result.Fail("Not found any appointment.");

                logger.InfoFormat("appointment name : {0}", appointment.Name);

                if (!string.IsNullOrEmpty(appointment.CalendarId))
                {
                    logger.InfoFormat("tenantId: {0}", workingContext.TenantId.HasValue ? workingContext.TenantId : null);
                    var calendarSync = GetAccountsByServiceType(workingContext.TenantId.ToString(), workingContext.UserId);
                    logger.InfoFormat("calendarSync: {0}", calendarSync != null && calendarSync.Any() ? "true" : "false");
                    foreach (var item in calendarSync.GroupBy(q => q.Provider).Select(t => t.Key))
                    {
                        ICalendarService CalendarService = CalendarFactory.Create(item);
                        var accountsPerProvider = calendarSync.Where(q => q.Provider == item).ToList();
                        CalendarService.DeleteAppointment(accountsPerProvider, appointment.CalendarId);
                    }
                }

                var totalActivity = totalActivityRepository.Value.GetAll().FirstOrDefault(c => c.ActivityId == appointment.Id);
                if (totalActivity != null)
                    TotalActivityService.Value.Delete(totalActivity.Id);

                var deleteResult = base.Delete(id);
                if (!string.IsNullOrEmpty(appointment.JobId))
                {
                    RemovePreviousReminders(appointment.JobId);
                }
                return deleteResult;
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.Message);
            }
        }

        public override IResponce SearchRequest<T>(string exp)
        {
            if (string.IsNullOrWhiteSpace(exp))
                return new QueryResponce();

            var result = TaskRepository.Value.SearchAppointmentsAndTasks(exp.Trim());

            var queryResponce = new QueryResponce()
            {
                Data = result.ToList(),
                PageCount = result.Count(),
                PageNo = 1,
                Total = result.Count()
            };

            return queryResponce;
        }

        public override Result SaveViaFlatWithResult<T>(FlatAppointment flat)
        {
            var appointment = appointmentRepository.Get(flat.Id);
            UserInfo currentUser = null;
            UserInfo owner = null;
            List<TotalActivity> appontmentList = null;
            List<ReferenceDefine> addUpdatedAttendess = null;
            Dictionary<string, string> jobList = new Dictionary<string, string>();
            Dictionary<string, string> removedJobs = new Dictionary<string, string>();

            if (flat.AppointmentType == AppointmentType.Web)
            {
                currentUser = UserInfoRepository.Value.Get(flat.CreatedBy);
                flat.Owner = new Lookup { Id = Guid.Parse(currentUser.Id.ToString()) };
                owner = currentUser;
            }
            else
                currentUser = UtilityService.Value.GetCurrentUser().Value;

            try
            {
                if (flat.Contract != null)
                {
                    var result = personService.Value.AddContractsForAppointment(flat.Contract);
                    flat.Description = flat.Description?.Replace("{Contract Link}", personService.Value.GetAppointmentLink(flat.Contract.Id, result.Value));
                }
            }
            catch (Exception)
            {
            }


            Guid ownerId = Guid.Empty;
            if (flat.AppointmentType != AppointmentType.Web && Guid.TryParse(flat.Owner.Id.ToString(), out ownerId))
                owner = UserInfoRepository.Value.Get(ownerId);
            else if (flat.AppointmentType != AppointmentType.Web)
            {
                return Result.Fail("Owner is not valid.");
            }

            var conflictedAppointments = GetSpecificAppointments(owner.Id, flat.StartDateTime, flat.Duration, flat.Id);
            //if (conflictedAppointments != null && conflictedAppointments.Any())
            //{
            //    return Result.Fail("Another appointment has already been booked in this time duration.Please choose another Date/Time.");
            //}
            bool isNew = appointment == null;
            DateTime dateTime = DateTime.ParseExact(flat.StartDateTimeSpan,
                                  "hh:mm tt", CultureInfo.InvariantCulture);
            TimeSpan span = dateTime.TimeOfDay;
            bool notSendSMSByNotity = false;
            try
            {
                TenantDetailsModel tenantInfo = null;
                var tenantId = flat.AppointmentType != AppointmentType.Web ? workingContext.TenantId : flat.TenantId;
                tenantInfo = TenantHttpClient.Value.GetTenantDetail(tenantId);
                tenantInfo.Logo = flat.BusinessLogoURL;
                var variablesList = new Dictionary<string, string>
                {
                    { "Owner_Name_Value", owner.Name },
                    { "Owner_Email_Value", owner.Email },
                    { "Owner_Phone_Value", owner.Phone },

                    { "{Contact Email}", string.Empty },
                    { "{Appointment Name}", string.Empty },
                    { "{Meeting Head Line}", string.Empty },
                    { "{Appointment Creation Date}", string.Empty }
                };

                if (appointment == null)
                {
                    appointment = new Appointment();
                    appointment.State = flat.State;
                    appointment.Id = flat.Id;
                    if (flat.AppointmentType == AppointmentType.Web)
                    {
                        appointment.CreatedBy = currentUser;
                        appointment.LastModifiedBy = currentUser;
                        appointment.CreationDate = DateTime.UtcNow;
                        appointment.LastModifiedDate = DateTime.UtcNow;
                    }
                    else
                    {
                        appointment.CreatedBy = currentUser;
                        appointment.CreationDate = DateTime.UtcNow;
                        appointment.LastModifiedDate = DateTime.UtcNow;
                        appointment.LastModifiedBy = currentUser;
                    }



                    var identityInfo = customerService.GetById(currentUser.UserId);
                    currentUser.Name = $" {identityInfo.FirstName} {identityInfo.LastName}";
                    currentUser.Email = identityInfo.Email;
                    currentUser.Phone = PhoneNumberHelper.GetE164(identityInfo.WorkPhoneNumber);
                    notificationManager = GetConfigNotification(NotificationTrigger.AppointmentCreated);
                    if (flat.Attendees != null)
                    {
                        flat.Attendees.ForEach(c => totalActivityRepository.Value.Insert(new TotalActivity
                        {
                            Id = Guid.NewGuid(),
                            BusinessObjectType = (BusinessObjectType)c.TypeName,
                            ActivityType = Cloud.BaseObject.BaseBusinessObjects.Enums.ActivityType.Appointment,
                            BusinessTypeObjectId = c.ReferenceId,
                            ActivityId = appointment.Id,
                            CreatedBy = currentUser,
                            CreationDate = DateTime.UtcNow,
                            LastModifiedBy = currentUser,
                            LastModifiedDate = DateTime.UtcNow
                        }));
                    }

                    try
                    {
                        logger.InfoFormat("Befor if for send email to attendess.");
                        variablesList.Add("EmailTemplateType", EmailTemplateType.Appointment.ToString());
                        var emails = SendNofificationAndReminder(flat, appointment, currentUser, span, ref notSendSMSByNotity, ref tenantInfo, tenantId, ref jobList, variablesList);

                        logger.InfoFormat("Befor sync email.");
                        if (!flat.NeedConfirmation)
                        {
                            SyncCalendar(flat, emails, GoogleCrud.Insert, owner);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.InfoException($"Error in Sync With this app id : {appointment.Id}", ex);
                    }

                }
                else
                {
                    appointment.HistoryTrackingValue = new List<FlatHistoryTrackingValue>();
                    notificationManager = GetConfigNotification(NotificationTrigger.AppointmentUpdated);

                    appontmentList = totalActivityRepository.Value.GetAll().Where(c => c.ActivityId == appointment.Id).ToList();
                    addUpdatedAttendess = flat.Attendees.Where(c => !appontmentList.Select(a => a.BusinessTypeObjectId).Contains(c.ReferenceId)).ToList();
                    appointment.LastModifiedDate = DateTime.UtcNow;
                    appointment.LastModifiedBy = currentUser;

                    if (!string.IsNullOrEmpty(appointment.JobId))
                    {
                        try
                        {
                            jobList = JsonConvert.DeserializeObject<Dictionary<string, string>>(appointment.JobId);
                        }
                        catch (Exception ex)
                        {
                            logger.InfoException($"Error in Reminders : {appointment.Id}", ex);
                        }
                    }
                    FlatLead flatLead = null;
                    IList<string> emails = new List<string>();
                    try
                    {
                        foreach (var item in flat.Attendees)
                        {
                            flatLead = GetEmailAttendees1((BusinessObjectType)item.TypeName, item.ReferenceId);
                            if (flatLead != null)
                            {
                                emails.Add(flatLead.Communication.Email);
                                Attendies.Add(flatLead);
                            }
                        }

                        if (!string.IsNullOrEmpty(flat.CalendarId))
                        {
                            SyncCalendar(flat, emails, GoogleCrud.Update, owner);
                        }

                    }
                    catch (Exception ex)
                    {
                        logger.InfoException($"Error in Sync With this app id : {appointment.Id}", ex);
                    }
                    #region AppointmentHasChange
                    AppointmentChangeTracking(flat, appointment, currentUser, owner, appontmentList, ref jobList, removedJobs, span, ref notSendSMSByNotity, ref tenantInfo, tenantId, variablesList);
                    if (removedJobs.Any())
                    {
                        RemovePreviousReminders(JsonConvert.SerializeObject(removedJobs));
                    }
                    appointment.JobId = JsonConvert.SerializeObject(jobList);
                    #endregion AppointmentHasChange
                }
                if (!isNew)
                {
                    if (appointment.NotifyByEmail != flat.NotifyByEmail)
                    {
                        HistoryTracking.GenerateCRMHistoryTrackingValue(
                            appointment.HistoryTrackingValue,
                            flat.NotifyByEmail.ToString(),
                            appointment.NotifyByEmail.ToString(),
                            nameof(appointment.NotifyByEmail),
                            appointment.NotifyByEmail.GetType().Name);
                    }
                    if (appointment.NotifyBySMS != flat.NotifyBySMS)
                    {
                        HistoryTracking.GenerateCRMHistoryTrackingValue(
                            appointment.HistoryTrackingValue,
                            flat.NotifyBySMS.ToString(),
                            appointment.NotifyBySMS.ToString(),
                            nameof(appointment.NotifyBySMS),
                            appointment.NotifyBySMS.GetType().Name);
                    }
                    if (!string.IsNullOrEmpty(appointment.Name) && appointment.Name != flat.Name)
                    {
                        HistoryTracking.GenerateCRMHistoryTrackingValue(
                            appointment.HistoryTrackingValue,
                            flat.Name.ToString(),
                            appointment.Name.ToString(),
                            nameof(appointment.Name),
                            appointment.Name.GetType().Name);
                    }
                    if (appointment.OwnerId != null && appointment.OwnerId != Guid.Empty && appointment.OwnerId != owner.Id)
                    {
                        HistoryTracking.GenerateCRMHistoryTrackingValue(
                        appointment.HistoryTrackingValue,
                        owner.Name,
                        appointment.Owner.Name,
                        nameof(appointment.Owner),
                        appointment.Owner.GetType().Name);
                    }
                }
                appointment.NotifyByEmail = flat.NotifyByEmail;
                appointment.Location = flat.Location;
                appointment.NotifyBySMS = flat.NotifyBySMS;
                appointment.StartDateTime = flat.StartDateTime;
                appointment.StartDateTimeSpan = flat.StartDateTimeSpan;
                appointment.AppointmentConfigId = flat.AppointmentConfigId.HasValue ? flat.AppointmentConfigId.Value : appointment.AppointmentConfigId;
                appointment.Duration = flat.Duration;
                appointment.CancelToken = flat.CancelToken;
                appointment.ReminderDateSpan = flat.ReminderDateSpan;
                appointment.SacondReminderDateSpan = flat.SacondReminderDateSpan;
                appointment.Name = flat.Name;
                appointment.Color = (string.IsNullOrEmpty(flat.Color) || flat.Color == "0") ? "#a4bdfc" : flat.Color;
                appointment.AppointmentType = flat.AppointmentType != 0 ? flat.AppointmentType : appointment.AppointmentType;
                appointment.Description = flat.Description;
                appointment.TimeZone = flat.TimeZone;
                appointment.CalendarId = flat.CalendarId;
                appointment.CustomQuestions = flat.CustomQuestions ?? appointment.CustomQuestions;
                appointment.OwnerId = owner.Id;
                Guid appointmentColorTypeId = Guid.Empty;
                if (flat.AppointmentColorType != null && flat.AppointmentColorType.Id != null && Guid.TryParse(flat.AppointmentColorType.Id.ToString(), out appointmentColorTypeId))
                {
                    if (!isNew && appointment.AppointmentColorTypeId != appointmentColorTypeId && appointment.AppointmentColorTypeId != null && appointment.AppointmentColorTypeId != Guid.Empty)
                    {
                        HistoryTracking.GenerateCRMHistoryTrackingValue(
                        appointment.HistoryTrackingValue,
                        flat.AppointmentColorType.Value?.ToString(),
                        appointment.AppointmentColorType.Color,
                        nameof(appointment.AppointmentColorType),
                        appointment.AppointmentColorType.GetType().Name);
                    }
                    appointment.AppointmentColorTypeId = appointmentColorTypeId;
                }
                else if (appointment.AppointmentColorTypeId != null && appointment.AppointmentColorTypeId != Guid.Empty)
                {
                    HistoryTracking.GenerateCRMHistoryTrackingValue(
                        appointment.HistoryTrackingValue,
                        null,
                        appointment.AppointmentColorType.Color,
                        nameof(appointment.AppointmentColorType),
                        appointment.AppointmentColorType.GetType().Name);
                }



                if (isNew)
                {
                    if (flat.AppointmentType == AppointmentType.Web)
                        appointmentRepository.InsertByTenantId(appointment, currentUser.Tenant.Value);
                    else
                    {
                        appointmentRepository.Insert(appointment);
                    }
                }
                else
                {
                    if (appointment.State != flat.State)
                    {
                        appointment.State = flat.State;
                        //use UpdateStateByUser method insted 
                        if (flat.State == AppointmentState.Approve)
                            UpdateState(appointment);
                    }

                    appointmentRepository.Update(appointment);

                    UpdatedAttendess(flat, appointment, currentUser, span, tenantInfo, appontmentList, addUpdatedAttendess, variablesList);
                    if (appointment.HistoryTrackingValue.Any())
                    {
                        var hostoryResult = _CrmHistoryService.Value.SaveViaFlatList(appointment.HistoryTrackingValue, appointment.Id, BusinessObjectType.Appointment);
                    }

                }
            }
            catch (Exception ex)
            {
                logger.InfoException("Error : ", ex);
                return Result.Fail(ex.Message);
            }

            //logger.InfoFormat("notSendSMSByNotity {0} : ", notSendSMSByNotity);
            //if (flat.NotifyBySMS && notSendSMSByNotity) return Result.Ok("SMS notification didn't send for client! Please check client phone number.");


            //if (conflictedAppointments != null && conflictedAppointments.Any())
            //    return Result.Ok<string>("Another appointment has already been booked in this time duration.");
            //else
            SendNotification(appointment, currentUser);
            return Result.Ok<Guid>(appointment.Id);
        }

        private void AppointmentChangeTracking(FlatAppointment flat, Appointment appointment, UserInfo currentUser, UserInfo owner, List<TotalActivity> appontmentList, ref Dictionary<string, string> jobList, Dictionary<string, string> removedJobs, TimeSpan span, ref bool notSendSMSByNotity, ref TenantDetailsModel tenantInfo, Guid? tenantId, Dictionary<string, string> variablesList)
        {
            List<string> changeTitles = new List<string>();
            variablesList.Add("EmailTemplateType", EmailTemplateType.UpdateAppointment.ToString());//EmailTemplateType.UpdateAppointment
            bool hasChange = false;
            bool needUpdateReminder = false;
            if (appointment.Owner.Id.CompareTo(Guid.Parse(flat.Owner.Id.ToString())) != 0)
            {
                variablesList.Add("{preOwnerName}", appointment.Owner.Name);
                changeTitles.Add("Owner");
                hasChange = true;
            }
            else { variablesList.Add("{preOwnerName}", string.Empty); }
            if (string.Compare(appointment.Location, flat.Location) != 0)
            {
                variablesList.Add("{preLocation}", string.IsNullOrEmpty(appointment.Location) ? string.Empty : appointment.Location.ToString());
                changeTitles.Add("Location");
                hasChange = true;
            }
            else { variablesList.Add("{preLocation}", string.Empty); }
            if (DateTime.Compare(appointment.StartDateTime, flat.StartDateTime) != 0)
            {
                variablesList.Add("{preFullDate}", appointment.StartDateTime.ToLongDateString());
                var time = $"{appointment.StartDateTime.Hour:D2}:{appointment.StartDateTime.Minute:D2} " +
                               $"{(appointment.StartDateTime.Hour > 12 ? "PM" : "AM")}";
                variablesList.Add("{preTime}", time);
                changeTitles.Add("Time");
                hasChange = true;
                needUpdateReminder = true;
            }
            else
            {
                variablesList.Add("{preFullDate}", string.Empty);
                variablesList.Add("{preTime}", string.Empty);
            }
            if (string.Compare(appointment.Description, flat.Description) != 0)
            {
                changeTitles.Add("Description");
            }
            if (TimeSpan.Compare(appointment.Duration, flat.Duration) != 0)
            {
                variablesList.Add("{preDuration}", $"{appointment.Duration.Hours:D2}:{appointment.Duration.Minutes:D2}");
                changeTitles.Add("Duration");
                hasChange = true;
            }
            else
            {
                variablesList.Add("{preDuration}", string.Empty);
            }
            if (TimeSpan.Compare(appointment.ReminderDateSpan, flat.ReminderDateSpan) != 0)
            {
                variablesList.Add("ReminderDateSpan", true.ToString());
                foreach (var kvp in jobList.Where(s => s.Key.Contains("first")).ToList())
                {
                    removedJobs.Add(kvp.Key, kvp.Value);
                    jobList.Remove(kvp.Key);
                }
            }
            if (TimeSpan.Compare(appointment.SacondReminderDateSpan, flat.SacondReminderDateSpan) != 0)
            {
                variablesList.Add("SecondReminderDateSpan", true.ToString());
                foreach (var kvp in jobList.Where(s => s.Key.Contains("second")).ToList())
                {
                    removedJobs.Add(kvp.Key, kvp.Value);
                    jobList.Remove(kvp.Key);
                }
            }
            if (hasChange) // todo just send to updateds..............
            {
                try
                {

                    if (appontmentList != null && appontmentList.Any())
                    {
                        variablesList.Add("Attendees_Existed", JsonConvert.SerializeObject(appontmentList.Select(s => s.BusinessTypeObjectId)));
                    }
                    variablesList.Add("{title}", string.Join(",", changeTitles.ToArray()));
                    variablesList.Add("{footerText}", string.Empty);//todo to write footer
                                                                    //variablesList.Add("{footerText}", "You are receiving this email because you have successfully subscribed to RunSensible's calendar by the organizer. RunSensible Calendar is a service provided by RunSensible.");//todo to write footer
                                                                    //variablesList.Add("{footerText}", "You are receiving this email because you have successfully subscribed to RunSensible's calendar by the organizer.\r\n                                                                            RunSensible Calendar is a service provided by RunSensible.");//todo to write footer
                    logger.InfoFormat("Befor if for send email to attendess.");
                    var emails2 = SendNofificationAndReminder(flat, appointment, currentUser, span, ref notSendSMSByNotity, ref tenantInfo, tenantId, ref jobList, variablesList, setReminder: needUpdateReminder);
                    logger.InfoFormat("Befor sync email.");
                    SyncCalendar(flat, emails2, GoogleCrud.Update, owner);
                }
                catch (Exception ex)
                {
                    logger.InfoException($"Error in Sync With this resend app id  : {appointment.Id}", ex);
                }
            }
            else if ((appointment.State == AppointmentState.None || appointment.State == AppointmentState.Approve) &&
                (flat.ReminderDateSpan != TimeSpan.Zero || flat.SacondReminderDateSpan != TimeSpan.Zero) &&
                (variablesList.ContainsKey("ReminderDateSpan") || variablesList.ContainsKey("SecondReminderDateSpan")))
            {
                AddReminder(flat, appointment, currentUser, jobList, span, tenantInfo, tenantId, variablesList);
            }
        }

        private Dictionary<string, string> AddReminder(FlatAppointment flat, Appointment appointment, UserInfo currentUser, Dictionary<string, string> jobList, TimeSpan span, TenantDetailsModel tenantInfo, Guid? tenantId, Dictionary<string, string> variablesList)
        {
            IList<EmailTemplate> emailTemplates = new List<EmailTemplate>();
            IList<string> totalAttendess = new List<string>();
            emailTemplates.Add(EmailTemplateRepository.Value.GetEmailTemplateByTemplateType(EmailTemplateType.ClientReminder));
            emailTemplates.Add(EmailTemplateRepository.Value.GetEmailTemplateByTemplateType(EmailTemplateType.OwnerReminder));
            //todo update reminder
            var dateReminderDateSpan = flat.ReminderDateSpan == TimeSpan.Zero
                                          ? (DateTimeOffset?)null
                                          : flat.StartDateTime.Add(-flat.ReminderDateSpan);

            var dateSacondReminderDateSpan = flat.SacondReminderDateSpan == TimeSpan.Zero
                                                      ? (DateTimeOffset?)null
                                                      : flat.StartDateTime.Add(-flat.SacondReminderDateSpan);
            var reminderNotification = GetConfigNotification(NotificationTrigger.AppointmentReminderToClient);
            var _CustomEmailTemplateConfig = EmailTemplateRepository.Value.GetEmailTemplateByTemplateType(EmailTemplateType.CustomEmailTemplate);//{CustomEmailContext}
            foreach (var item in flat.Attendees.ToList())
            {
                //todo check if Atterndies already have this content
                var flatLeadForReminder = GetEmailAttendees1((item.TypeName == 0 ? item.BusinessObjectType : (BusinessObjectType)item.TypeName), item.ReferenceId);
                //var flatLead = GetEmailAttendees((BusinessObjectType)item.TypeName, item.ReferenceId);
                var sms = (reminderNotification == null || string.IsNullOrEmpty(reminderNotification.CustomSmsContext)) ? null : SetPropertiesToCustomeBody(reminderNotification.CustomSmsContext, appointment, flat, flatLeadForReminder, tenantInfo, objectVariableList: variablesList);
                var email = (reminderNotification == null || string.IsNullOrEmpty(reminderNotification.CustomEmailContext)) ? null : SetPropertiesToCustomeBody(reminderNotification.CustomEmailContext, appointment, flat, flatLeadForReminder, tenantInfo, objectVariableList: variablesList);
                if (flatLeadForReminder != null)
                {
                    var _customEmailTemplateContextReminder = _CustomEmailTemplateConfig.Body;
                    _customEmailTemplateContextReminder = SetPropertiesToCustomeBody(_customEmailTemplateContextReminder, appointment, flat, flatLeadForReminder, tenantInfo, objectVariableList: variablesList);
                    totalAttendess.Add(flatLeadForReminder.Name);
                    if ((variablesList.ContainsKey("ReminderDateSpan")) && flat.ReminderDateSpan != TimeSpan.Zero)
                    {
                        logger.InfoFormat("Date Span: {0}", dateReminderDateSpan.Value);

                        var firstReminderCommandClient = new AppointmentReminderToClientCommand()
                        {
                            Attendess = flatLeadForReminder,
                            TenantDetailsModel = tenantInfo,
                            AppointmentName = appointment.Name,
                            TenantId = workingContext.TenantId.Value,
                            ReminderDateSpan = dateReminderDateSpan.Value,
                            FlatAppointment = flat,
                            Span = span,
                            Owner = currentUser,
                            CompanyName = tenantInfo != null ? tenantInfo.Name : string.Empty,
                            TempClientReminder = emailTemplates.SingleOrDefault(c => c.EmailTemplateType == EmailTemplateType.ClientReminder)?.Body,
                            CustomSmsContext = sms,
                            CustomEmailContext = string.IsNullOrEmpty(email) ? null : _customEmailTemplateContextReminder.Replace("{CustomEmailContext}", email)
                        };
                        var result = Task.Run(async () => await Mediator.Value.Send(firstReminderCommandClient)).Result;
                        jobList.Add("first" + flatLeadForReminder.Id, result.JobId);
                    }

                    if ((variablesList.ContainsKey("SecondReminderDateSpan")) && flat.SacondReminderDateSpan != TimeSpan.Zero)
                    {
                        var secondReminderCommandClient = new AppointmentReminderToClientCommand()
                        {
                            Attendess = flatLeadForReminder,
                            TenantDetailsModel = tenantInfo,
                            AppointmentName = appointment.Name,
                            TenantId = workingContext.TenantId.Value,
                            ReminderDateSpan = dateSacondReminderDateSpan.Value,
                            FlatAppointment = flat,
                            Span = span,
                            Owner = currentUser,
                            CompanyName = tenantInfo != null ? tenantInfo.Name : string.Empty,
                            TempClientReminder = emailTemplates.SingleOrDefault(c => c.EmailTemplateType == EmailTemplateType.ClientReminder)?.Body,
                            CustomSmsContext = sms,
                            CustomEmailContext = string.IsNullOrEmpty(email) ? null : _customEmailTemplateContextReminder.Replace("{CustomEmailContext}", email)
                        };
                        var result = Task.Run(async () => await Mediator.Value.Send(secondReminderCommandClient)).Result;
                        jobList.Add("second" + flatLeadForReminder.Id, result.JobId);
                    }
                }
            }

            if ((variablesList.ContainsKey("ReminderDateSpan")) && flat.ReminderDateSpan != TimeSpan.Zero)
            {
                var firstReminderCommand = new AppointmentReminderToClientCommand()
                {
                    AttendessList = string.Join(",", totalAttendess),
                    TenantDetailsModel = tenantInfo,
                    AppointmentName = appointment.Name,
                    TenantId = workingContext.TenantId.Value,
                    ReminderDateSpan = dateReminderDateSpan.Value,
                    EmailOwner = currentUser.Email,
                    PhoneOwner = currentUser.Phone,
                    Owner = currentUser,
                    CompanyName = tenantInfo != null ? tenantInfo.Name : string.Empty,
                    FlatAppointment = flat,
                    Span = span,
                    TempOwnerReminder = emailTemplates.SingleOrDefault(c => c.EmailTemplateType == EmailTemplateType.OwnerReminder)?.Body
                };
                var result = Task.Run(async () => await Mediator.Value.Send(firstReminderCommand)).Result;
                jobList.Add("first" + currentUser.Id, result.JobId);
            }

            if ((variablesList.ContainsKey("SecondReminderDateSpan")) && flat.SacondReminderDateSpan != TimeSpan.Zero)
            {
                if (tenantInfo == null)
                    tenantInfo = TenantHttpClient.Value.GetTenantDetail(tenantId);

                var secondReminderCommand = new AppointmentReminderToClientCommand()
                {
                    AttendessList = string.Join(",", totalAttendess),
                    TenantDetailsModel = tenantInfo,
                    AppointmentName = appointment.Name,
                    TenantId = workingContext.TenantId.Value,
                    ReminderDateSpan = dateSacondReminderDateSpan.Value,
                    EmailOwner = currentUser.Email,
                    PhoneOwner = currentUser.Phone,
                    Owner = currentUser,
                    CompanyName = tenantInfo != null ? tenantInfo.Name : string.Empty,
                    FlatAppointment = flat,
                    Span = span,
                    TempOwnerReminder = emailTemplates.SingleOrDefault(c => c.EmailTemplateType == EmailTemplateType.OwnerReminder)?.Body
                };
                var result = Task.Run(async () => await Mediator.Value.Send(secondReminderCommand)).Result;
                jobList.Add("second" + currentUser.Id, result.JobId);
            }
            return jobList;
            //return tenantInfo;
        }

        private void RemovePreviousReminders(string jobId)
        {
            try
            {
                var removeReminder = new AppointmentDeleteReminderToClientCommand()
                {
                    JobId = jobId
                };
                var result = Task.Run(async () => await Mediator.Value.Send(removeReminder)).Result;
                if (!result.Success)
                {
                    logger.ErrorFormat("Jobs was not deleted successfully: {0}", jobId);
                }
            }
            catch (Exception ex)
            {
                logger.ErrorFormat("RemovepreviousReminders: {0}", jobId);
            }
        }
        private void UpdatedAttendess(FlatAppointment flat, Appointment appointment, UserInfo currentUser, TimeSpan span, TenantDetailsModel tenantInfo,
            List<TotalActivity> appontmentList, List<ReferenceDefine> addUpdatedAttendess, Dictionary<string, string> variablesList)
        {
            if (variablesList != null && variablesList.Any() && variablesList.ContainsKey("EmailTemplateType"))
            {
                variablesList["EmailTemplateType"] = EmailTemplateType.Appointment.ToString();
            }
            /*
            var appontmentList = totalActivityRepository.GetAll().Where(c => c.ActivityId == appointment.Id).ToList();

            var addUpdatedAttendess = flat.Attendees.Where(c => !appontmentList.Select(a => a.BusinessTypeObjectId).Contains(c.ReferenceId)).ToList();
            */
            FlatLead flatLead = null;

            if (addUpdatedAttendess.Any())
            {
                var identityInfo = customerService.GetById(currentUser.UserId);
                currentUser.Name = $" {identityInfo.FirstName} {identityInfo.LastName}";
                currentUser.Email = identityInfo.Email;
            }

            foreach (var attendess in addUpdatedAttendess)
            {
                totalActivityRepository.Value.Insert(new TotalActivity
                {
                    Id = Guid.NewGuid(),
                    BusinessObjectType = (BusinessObjectType)attendess.TypeName,
                    ActivityType = Cloud.BaseObject.BaseBusinessObjects.Enums.ActivityType.Appointment,
                    BusinessTypeObjectId = attendess.ReferenceId,
                    ActivityId = appointment.Id,
                    CreatedBy = currentUser,
                    CreationDate = DateTime.UtcNow,
                    LastModifiedBy = currentUser,
                    LastModifiedDate = DateTime.UtcNow
                });

                flatLead = GetEmailAttendees((BusinessObjectType)attendess.TypeName, attendess.ReferenceId);
                if (flatLead != null)
                {
                    if (flat.NotifyByEmail || (notificationManager != null && notificationManager.Value % (int)NotificationDestination.Email == 0))
                    {
                        SendEmailToRecipaint(flat, tenantInfo,
                                                 span,
                                                 flatLead,
                                                 null,
                                                 currentUser,
                                                 workingContext.TenantId.Value,
                                                 true,
                                                 isSystem: true,
                                                 appAppointment: true, objectVariableList: variablesList);
                    }
                }
            }

            for (int i = 0; i < appontmentList.Count; i++)
            {
                if (flat.Attendees.All(s => s.ReferenceId != appontmentList[i].BusinessTypeObjectId))
                {
                    try
                    {
                        appointment.HistoryTrackingValue.Add(new FlatHistoryTrackingValue
                        {
                            NewValue = null,
                            OldValue = GetEmailAttendees((BusinessObjectType)appontmentList[i].BusinessObjectType, appontmentList[i].BusinessTypeObjectId)?.Name,
                            PropertyType = "Clients",
                            Title = "Clients"
                        });
                    }
                    catch (Exception e)
                    {

                    }
                    totalActivityRepository.Value.DeleteById(appontmentList[i].Id);
                }
            }
        }

        private IList<string> SendNofificationAndReminder(FlatAppointment flat, Appointment appointment, UserInfo currentUser, TimeSpan span, ref bool notSendSMSByNotity, ref TenantDetailsModel tenantInfo, Guid? tenantId, ref Dictionary<string, string> jobList, Dictionary<string, string> objectVariableList = null, bool setReminder = true)
        {
            IList<string> totalAttendess = new List<string>();
            IList<string> emails = new List<string>();


            IList<EmailTemplate> emailTemplates = new List<EmailTemplate>();
            emailTemplates.Add(EmailTemplateRepository.Value.GetEmailTemplateByTemplateType(EmailTemplateType.ClientReminder));
            emailTemplates.Add(EmailTemplateRepository.Value.GetEmailTemplateByTemplateType(EmailTemplateType.OwnerReminder));

            var dateReminderDateSpan = flat.ReminderDateSpan == TimeSpan.Zero
                                                      ? (DateTimeOffset?)null
                                                      : flat.StartDateTime.Add(-flat.ReminderDateSpan);

            var dateSacondReminderDateSpan = flat.SacondReminderDateSpan == TimeSpan.Zero
                                                      ? (DateTimeOffset?)null
                                                      : flat.StartDateTime.Add(-flat.SacondReminderDateSpan);
            var flatAttendees = flat.Attendees != null ? flat.Attendees.ToList() : new List<ReferenceDefine>();
            if (objectVariableList != null && objectVariableList.Any() && objectVariableList.ContainsKey("Attendees_Existed"))
            {
                var existAttendees = JsonConvert.DeserializeObject<List<Guid>>(objectVariableList["Attendees_Existed"]);
                flatAttendees = flat.Attendees.Where(d => existAttendees.Contains(d.ReferenceId)).ToList();
            }
            if (flatAttendees != null && flatAttendees.Any())
            {
                var _CustomEmailTemplateConfig = EmailTemplateRepository.Value.GetEmailTemplateByTemplateType(EmailTemplateType.CustomEmailTemplate);

                var reminderNotification = GetConfigNotification(NotificationTrigger.AppointmentReminderToClient);
                foreach (var item in flatAttendees)
                {
                    var flatLead = GetEmailAttendees((item.TypeName == 0 ? item.BusinessObjectType : (BusinessObjectType)item.TypeName), item.ReferenceId);
                    string _customEmailTemplateContext = null;
                    if (notificationManager != null && !string.IsNullOrEmpty(notificationManager.CustomEmailContext))
                    {
                        _customEmailTemplateContext = _CustomEmailTemplateConfig.Body;
                        var bodyDetail = notificationManager.CustomEmailContext;
                        bodyDetail = SetPropertiesToCustomeBody(bodyDetail, appointment, flat, flatLead, tenantInfo, objectVariableList);
                        _customEmailTemplateContext = SetPropertiesToCustomeBody(_customEmailTemplateContext, appointment, flat, flatLead, tenantInfo, objectVariableList);
                        _customEmailTemplateContext = _customEmailTemplateContext.Replace("{CustomEmailContext}", bodyDetail);
                    }
                    if (flatLead != null)
                    {
                        notSendSMSByNotity = flatLead.Communication.FlatPhoneNumbers?.Any(c => string.IsNullOrEmpty(c.Number)) ?? false;
                        totalAttendess.Add(flatLead.Name);
                        emails.Add(flatLead.Communication.Email);

                        var attendess = flat.Attendees.First(x => x.ReferenceId == item.ReferenceId);
                        attendess.ReferenceName = flatLead.Name;
                        if (flat.AppointmentType != AppointmentType.Web && (flat.NotifyByEmail || (notificationManager != null && notificationManager.Value % (int)NotificationDestination.Email == 0)))
                        {
                            NotifyByEmailToRecipaint(flat, currentUser, tenantInfo, tenantId, true, span, flatLead, false, objectVariableList: objectVariableList, customeEmailBody: _customEmailTemplateContext);
                        }
                        else if (flat.AppointmentType == AppointmentType.Web && !flat.NeedConfirmation)
                        {

                            NotifyByEmailToRecipaint(flat, currentUser, tenantInfo, tenantId, true, span, flatLead);
                            NotifyByEmailToRecipaint(flat, currentUser, tenantInfo, tenantId, false, span, flatLead);
                        }
                        else if (flat.AppointmentType == AppointmentType.Web && flat.NeedConfirmation && !flat.HasWorkFlow.HasValue)
                        {
                            NotifyByEmailToRecipaintForApproveAppointment(flat, currentUser, tenantInfo, tenantId, true, span, flatLead, objectVariableList);
                            NotifyByEmailToRecipaintForApproveAppointment(flat, currentUser, tenantInfo, tenantId, false, span, flatLead, objectVariableList);
                        }

                        if (flat.HasWorkFlow.HasValue)
                        {
                            Task.Run(async delegate
                            {
                                await Task.Delay(600000);
                                try
                                {
                                    var appt = appointmentRepository.Get(flat.Id);

                                    if (appt != null && string.IsNullOrWhiteSpace(appt.WorkFlow) && appt.State == AppointmentState.Pending)
                                    {
                                        Delete(flat.Id);
                                    }
                                }
                                catch (Exception ex)
                                {

                                    logger.ErrorException("Error in final appointment: ", ex);
                                }
                            });
                        }

                        if (flat.AppointmentType != AppointmentType.Web && (flat.NotifyBySMS || (notificationManager != null && notificationManager.Value % (int)NotificationDestination.Mobile == 0)))
                        {
                            var phonenumber = flatLead.Communication.FlatPhoneNumbers.FirstOrDefault(c => c.PhoneType == PhoneType.Mobile);

                            if (phonenumber != null && !string.IsNullOrEmpty(phonenumber.Number))
                            {
                                var smsBody = GenerateSMSBody(tenantInfo, currentUser, flat);
                                if (notificationManager != null && !string.IsNullOrEmpty(notificationManager.CustomSmsContext))
                                {
                                    smsBody = SetPropertiesToCustomeBody(notificationManager.CustomSmsContext, appointment, flat, flatLead, tenantInfo, objectVariableList: objectVariableList);
                                }
                                SmsService.Value.SendSystemSMS(to: phonenumber.Number,
                                                         body: smsBody, tenant: workingContext.TenantId?.ToString());
                            }
                        }

                        if ((appointment.State == AppointmentState.None || appointment.State == AppointmentState.Approve) && (setReminder || objectVariableList.ContainsKey("ReminderDateSpan")) && flat.ReminderDateSpan != TimeSpan.Zero)
                        {

                            var sms = (reminderNotification == null || string.IsNullOrEmpty(reminderNotification.CustomSmsContext)) ? null : SetPropertiesToCustomeBody(reminderNotification.CustomSmsContext, appointment, flat, flatLead, tenantInfo, objectVariableList: objectVariableList);
                            var email = (reminderNotification == null || string.IsNullOrEmpty(reminderNotification.CustomEmailContext)) ? null : SetPropertiesToCustomeBody(reminderNotification.CustomEmailContext, appointment, flat, flatLead, tenantInfo, objectVariableList: objectVariableList);
                            var _customEmailTemplateContextReminder = _CustomEmailTemplateConfig.Body;
                            _customEmailTemplateContextReminder = SetPropertiesToCustomeBody(_customEmailTemplateContextReminder, appointment, flat, flatLead, tenantInfo, objectVariableList: objectVariableList);

                            var firstReminderCommandClient = new AppointmentReminderToClientCommand()
                            {
                                Attendess = flatLead,
                                TenantDetailsModel = tenantInfo,
                                AppointmentName = appointment.Name,
                                TenantId = workingContext.TenantId.Value,
                                ReminderDateSpan = dateReminderDateSpan.Value,
                                FlatAppointment = flat,
                                Span = span,
                                Owner = currentUser,
                                CompanyName = tenantInfo != null ? tenantInfo.Name : string.Empty,
                                TempClientReminder = emailTemplates.SingleOrDefault(c => c.EmailTemplateType == EmailTemplateType.ClientReminder)?.Body,
                                CustomSmsContext = sms,
                                CustomEmailContext = string.IsNullOrEmpty(email) ? null : _customEmailTemplateContextReminder.Replace("{CustomEmailContext}", email)

                            };
                            var result = Task.Run(async () => await Mediator.Value.Send(firstReminderCommandClient)).Result;
                            jobList.Add("first" + flatLead.Id, result.JobId);
                        }

                        if ((appointment.State == AppointmentState.None || appointment.State == AppointmentState.Approve) && (setReminder || objectVariableList.ContainsKey("SecondReminderDateSpan")) && flat.SacondReminderDateSpan != TimeSpan.Zero)
                        {
                            var sms = (reminderNotification == null || string.IsNullOrEmpty(reminderNotification.CustomSmsContext)) ? null : SetPropertiesToCustomeBody(reminderNotification.CustomSmsContext, appointment, flat, flatLead, tenantInfo, objectVariableList: objectVariableList);
                            var email = (reminderNotification == null || string.IsNullOrEmpty(reminderNotification.CustomEmailContext)) ? null : SetPropertiesToCustomeBody(reminderNotification.CustomEmailContext, appointment, flat, flatLead, tenantInfo, objectVariableList: objectVariableList);
                            var _customEmailTemplateContextReminder = _CustomEmailTemplateConfig.Body;
                            _customEmailTemplateContextReminder = SetPropertiesToCustomeBody(_customEmailTemplateContextReminder, appointment, flat, flatLead, tenantInfo, objectVariableList: objectVariableList);

                            var secondReminderCommandClient = new AppointmentReminderToClientCommand()
                            {
                                Attendess = flatLead,
                                TenantDetailsModel = tenantInfo,
                                AppointmentName = appointment.Name,
                                TenantId = workingContext.TenantId.Value,
                                ReminderDateSpan = dateSacondReminderDateSpan.Value,
                                FlatAppointment = flat,
                                Span = span,
                                Owner = currentUser,
                                CompanyName = tenantInfo != null ? tenantInfo.Name : string.Empty,
                                TempClientReminder = emailTemplates.SingleOrDefault(c => c.EmailTemplateType == EmailTemplateType.ClientReminder)?.Body,
                                CustomSmsContext = sms,
                                CustomEmailContext = string.IsNullOrEmpty(email) ? null : _customEmailTemplateContextReminder.Replace("{CustomEmailContext}", email)
                            };
                            var result = Task.Run(async () => await Mediator.Value.Send(secondReminderCommandClient)).Result;
                            jobList.Add("second" + flatLead.Id, result.JobId);
                        }
                    }
                }
            }

            if ((appointment.State == AppointmentState.None || appointment.State == AppointmentState.Approve) && (setReminder || objectVariableList.ContainsKey("ReminderDateSpan")) && flat.ReminderDateSpan != TimeSpan.Zero)
            {
                var firstReminderCommand = new AppointmentReminderToClientCommand()
                {
                    AttendessList = string.Join(",", totalAttendess),
                    TenantDetailsModel = tenantInfo,
                    AppointmentName = appointment.Name,
                    TenantId = workingContext.TenantId.Value,
                    ReminderDateSpan = dateReminderDateSpan.Value,
                    EmailOwner = currentUser.Email,
                    PhoneOwner = currentUser.Phone,
                    Owner = currentUser,
                    CompanyName = tenantInfo != null ? tenantInfo.Name : string.Empty,
                    FlatAppointment = flat,
                    Span = span,
                    TempOwnerReminder = emailTemplates.SingleOrDefault(c => c.EmailTemplateType == EmailTemplateType.OwnerReminder)?.Body
                };
                var result = Task.Run(async () => await Mediator.Value.Send(firstReminderCommand)).Result;
                jobList.Add("first" + currentUser.Id, result.JobId);
            }

            if ((appointment.State == AppointmentState.None || appointment.State == AppointmentState.Approve) && (setReminder || objectVariableList.ContainsKey("SecondReminderDateSpan")) && flat.SacondReminderDateSpan != TimeSpan.Zero)
            {
                if (tenantInfo == null)
                    tenantInfo = TenantHttpClient.Value.GetTenantDetail(tenantId);

                var secondReminderCommand = new AppointmentReminderToClientCommand()
                {
                    AttendessList = string.Join(",", totalAttendess),
                    TenantDetailsModel = tenantInfo,
                    AppointmentName = appointment.Name,
                    TenantId = workingContext.TenantId.Value,
                    ReminderDateSpan = dateSacondReminderDateSpan.Value,
                    EmailOwner = currentUser.Email,
                    PhoneOwner = currentUser.Phone,
                    Owner = currentUser,
                    CompanyName = tenantInfo != null ? tenantInfo.Name : string.Empty,
                    FlatAppointment = flat,
                    Span = span,
                    TempOwnerReminder = emailTemplates.SingleOrDefault(c => c.EmailTemplateType == EmailTemplateType.OwnerReminder)?.Body
                };
                var result = Task.Run(async () => await Mediator.Value.Send(secondReminderCommand)).Result;
                jobList.Add("second" + currentUser.Id, result.JobId);
            }

            return emails;
        }

        private void NotifyByEmailToRecipaint(FlatAppointment flat,
                                UserInfo currentUser, TenantDetailsModel tenantInfo,
                                Guid? tenantId, bool toRecipaint,
                                TimeSpan span, FlatLead flatLead, bool forApproveAppointment = false, EmailTemplateType? emailTemplateType = null, Dictionary<string, string> objectVariableList = null, string customeEmailBody = null)
        {
            try
            {


                SendEmailToRecipaint(flat, tenantInfo,
                                  span,
                                  flatLead,
                                  null,
                                  currentUser,
                                  tenantId.Value,
                                  toRecipaint,
                                  isSystem: !toRecipaint,
                                  appAppointment: flat.AppointmentType != AppointmentType.Web, forApproveAppointment, emailTemplateType: emailTemplateType, objectVariableList: objectVariableList, customeEmailBody);

            }
            catch (Exception ex)
            {

                logger.InfoException("Error in NotifyByEmailToRecipaint: ", ex);
            }
        }

        private void NotifyByEmailToRecipaintForApproveAppointment(FlatAppointment flat,
                                UserInfo currentUser, TenantDetailsModel tenantInfo,
                                Guid? tenantId, bool toRecipaint,
                                TimeSpan span, FlatLead flatLead, Dictionary<string, string> objectVariableList = null)
        {
            try
            {


                SendEmailToRecipaintForAppointment(flat, tenantInfo,
                                  span,
                                  flatLead,
                                  null,
                                  currentUser,
                                  tenantId.Value,
                                  isSystem: !toRecipaint,
                                  appAppointment: flat.AppointmentType != AppointmentType.Web, objectVariableList);

            }
            catch (Exception ex)
            {

                logger.InfoException("Error in NotifyByEmailToRecipaint: ", ex);
            }
        }


        private string GenerateSMSBody(TenantDetailsModel tenantDetailsModel, UserInfo userInfo, FlatAppointment flat, bool fromWeb = false)
        {
            string template = string.Empty;
            if (!fromWeb)
                //                template = @"Good day!

                //This is to advise that an appointment booked with {ownerName}, {companyName}. Please contact us if you need to reschedule. Thank you!
                //Date: {date}
                //Time: {time}
                //Location: {location}
                //Note: {description}
                //Time zone: {timeZone}
                //                               ".Replace("{ownerName}", ownerName)
                //                                  .Replace("{companyName}", companyName)
                //                                  .Replace("{location}", !string.IsNullOrEmpty(flat.Location) ? flat.Location : string.Empty)
                //                                  .Replace("{description}", !string.IsNullOrEmpty(flat.Description) ? flat.Description : string.Empty)
                //                                  .Replace("{date}", flat.StartDateTime.Date.ToLongDateString())
                //                                  .Replace("{timeZone}", flat.TimeZone)
                //                                  .Replace("{time}", flat.StartDateTimeSpan);

                template = GenerateBodyExtension.SmsBodyWithoutApproveCustomer(tenantDetailsModel, userInfo, flat);
            else
            {
                //                template = @"Good day!

                //This is to advise that {lead} has scheduled a meeting appointment with you.

                //Date: {date}
                //Time:  {time}
                //Location:  {location}
                //Note: {description}

                //".Replace("{lead}", flat.Attendees.First().ReferenceName)
                //.Replace("{location}", !string.IsNullOrEmpty(flat.Location) ? flat.Location : string.Empty)
                //                                  .Replace("{description}", !string.IsNullOrEmpty(flat.Description) ? flat.Description : string.Empty)
                //                                  .Replace("{date}", flat.StartDateTime.Date.ToLongDateString())
                //                                  .Replace("{time}", flat.StartDateTimeSpan);

                template = GenerateBodyExtension.SmsBodyWithoutApproveOwner(tenantDetailsModel, userInfo, flat);

            }

            return template;
        }

        private void SyncCalendar(FlatAppointment flat, IList<string> attendees, GoogleCrud googleCrud, UserInfo userInfo)
        {
            var allcalendarSync = GetAccountsByServiceType(userInfo.Tenant.ToString(), userInfo.UserId.ToString());
            var calendarSync = allcalendarSync.Where(s => s.OwnerId == Guid.Parse(userInfo?.Id.ToString()) || s.OwnerId == Guid.Parse(userInfo?.UserId.ToString()));
            logger.InfoFormat("Count sync calendar {0}.", calendarSync.Count());

            if (calendarSync.Any())
            {
                var flatEvent = new FlatEvent
                {
                    StartDateTime = flat.StartDateTime,
                    Duration = flat.Duration,
                    ReminderDateSpan = flat.ReminderDateSpan,
                    Description = flat.Description,
                    Name = flat.Name,
                    Location = flat.Location,
                    //ColorId = flat.ColorId
                };

                foreach (var item in attendees)
                {
                    var isEmail = CrmExtention.CheckEmail(item);
                    if (!string.IsNullOrEmpty(item) && isEmail)
                        flatEvent.Attendees.Add(item);
                }

                string calendarId = string.Empty;
                foreach (var item in calendarSync.GroupBy(q => q.Provider).Select(t => t.Key))
                {
                    string cid = string.Empty;
                    ICalendarService CalendarService = CalendarFactory.Create(item);
                    var accountsPerProvider = calendarSync.Where(q => q.Provider == item).ToList();
                    if (googleCrud == GoogleCrud.Insert)
                        cid = CalendarService.InsertEvent(accountsPerProvider, flatEvent);
                    if (googleCrud == GoogleCrud.Update)
                        cid = CalendarService.UpdateEvent(accountsPerProvider, flatEvent, flat.CalendarId);
                    if (googleCrud == GoogleCrud.Delete)
                        CalendarService.DeleteAppointment(accountsPerProvider, flat.CalendarId);

                    if (string.IsNullOrEmpty(calendarId))
                    {
                        if (!string.IsNullOrWhiteSpace(cid))
                            calendarId = cid;
                    }
                    else if (!string.IsNullOrWhiteSpace(cid))
                        calendarId = calendarId + ";" + cid;
                }
                flat.CalendarId = calendarId;

            }
        }

        public Result<Guid> CreateMeetingFromWeb(FlatAppointmentFromWeb flat)
        {
            Guid appointmenId = Guid.Empty;

            try
            {

                var bussinesObj = JsonConvert.DeserializeObject<FlatLead>(flat.LeadProperties);
                if (!string.IsNullOrEmpty(bussinesObj.CompanyName))
                    bussinesObj.Company = new Lookup { Value = bussinesObj.CompanyName };
                var startDate = flat.MeetingBookedDate;
                var appintmentConfig = appointmentConfigRepository.Value.Get(flat.AppointmentConfigId);

                if (appintmentConfig == null)
                    return Result.Fail<Guid>("There is no appointmentConfig");

                var calendarSync = GetAccountsByServiceType(appintmentConfig.CreatedBy.Tenant.ToString(), appintmentConfig.CreatedBy.UserId.ToString());
                var tenantInfo = TenantHttpClient.Value.GetTenantDetail(appintmentConfig.Tenant);

                if (!calendarSync.Any())
                {
                    tenantInfo.Logo = flat.BusinessLogoURL;
                    return Result.Fail<Guid>($"There is something wrong with the appointment configuration. Please contact {tenantInfo.Phone}, or {tenantInfo.Email}.");
                }

                var appointments = appointmentRepository.GetAllByTenantId(appintmentConfig.Tenant.Value).ToList();

                var result = appointments.FirstOrDefault(c =>
                    (startDate <= c.StartDateTime && c.StartDateTime < startDate.Add(flat.Duration) &&
                     startDate.Add(flat.Duration) <= c.StartDateTime.Add(c.Duration)) ||
                    (c.StartDateTime <= startDate && startDate < c.StartDateTime.Add(c.Duration) &&
                     startDate.Add(flat.Duration) <= c.StartDateTime.Add(c.Duration)) ||
                    (c.StartDateTime <= startDate && startDate < c.StartDateTime.Add(c.Duration) &&
                     c.StartDateTime.Add(c.Duration) <= startDate.Add(flat.Duration)));

                if (result != null)
                    return Result.Fail<Guid>(
                        $"Looks like that time is no longer available. Please choose another convenient time.");

                var blockTimes =
                    JsonConvert.DeserializeObject<IList<FlatBlockTimesOff>>(appintmentConfig.AvailableTimes);
                var IsBlockTime = blockTimes.Any(c =>
                    c.BlockDate.Date == startDate.Add(flat.Duration).Date &&
                    c.FromTime <= startDate.Add(flat.Duration).TimeOfDay &&
                    c.ToTime >= startDate.Add(flat.Duration).TimeOfDay);

                if (IsBlockTime)
                    return Result.Fail<Guid>("This Time is Block Time");

                var RedirectUrl = ConfigurationManager.AppSettings["RedirectUrl"];

                var businessLogoUrl = tenantInfo.Logo;// $"{RedirectUrl}api/businessLogo/{appintmentConfig.Tenant.ToString()}";
                logger.InfoFormat("businessLogoUrl path:{0}", businessLogoUrl);

                var appointment = new FlatAppointment
                {
                    Id = flat.Id,
                    CancelToken = RandomString(20),
                    Name =
                        $"{bussinesObj.FirstName} {bussinesObj.LastName} with {appintmentConfig.CreatedBy.Name}",
                    StartDateTime = startDate,
                    //StartDateTimeSpan = flat.MeetingBookedTime,
                    Duration = flat.Duration,
                    AppointmentType = AppointmentType.Web,
                    CreatedBy = appintmentConfig.CreatedBy.Id,
                    CustomQuestions = flat.CustomQuestion,
                    StartDateTimeSpan = flat.MeetingBookedTime,
                    TenantId = appintmentConfig.Tenant.Value,
                    TimeZone = appintmentConfig.TimeZone,
                    BusinessLogoURL = businessLogoUrl,
                    State = appintmentConfig.ApproveRequired ? AppointmentState.Pending : AppointmentState.None,
                    NeedConfirmation = appintmentConfig.ApproveRequired,
                    AppointmentConfigId = appintmentConfig.Id,
                };

                if (appintmentConfig.AppointmentWorkFlow.Any(x => !string.IsNullOrWhiteSpace(x.Invoice)))
                {
                    appointment.State = AppointmentState.Pending;
                    appointment.HasWorkFlow = true;
                }

                var tempLead = LeadRepository.Value.GetLeadByEmail(bussinesObj.Communication.Email.Trim(), appintmentConfig.Tenant.Value);
                if (tempLead.Type == null)
                {

                    if (appintmentConfig.AppointmentWorkFlow.Count > 1)
                    {
                        var personObj = JsonConvert.DeserializeObject<FlatPerson>(flat.LeadProperties);
                        personObj.Owner = new Lookup { Id = appintmentConfig.CreatedBy.Id };
                        workingContext.TenantId = appintmentConfig.Tenant;
                        workingContext.UserId = appintmentConfig.CreatedBy.UserId.ToString();

                        var personResult = personService.Value.SaveViaFlatWithResult<FlatPerson>(personObj) as Result<Guid>;
                        bussinesObj.Id = personResult.Value;
                        tempLead.Type = BusinessObjectType.Person;
                    }
                    else
                    {
                        var leadState = LeadStateRepository.Value.GetList()
                      .FirstOrDefault(c => c.State == State.New);
                        bussinesObj.SentFromAppointment = true;
                        bussinesObj.Owner = new Lookup { Id = appintmentConfig.CreatedBy.Id };
                        if (leadState != null)
                            bussinesObj.LeadState = new Lookup { Id = leadState.Id };

                        LeadService.Value.SaveWithFlatLead(bussinesObj, false);
                    }
                }
                else
                {
                    if (tempLead.Type == BusinessObjectType.Lead && appintmentConfig.AppointmentWorkFlow.Count > 1)
                    {
                        workingContext.TenantId = appintmentConfig.Tenant;
                        workingContext.UserId = appintmentConfig.CreatedBy.UserId.ToString();

                        var personObj = JsonConvert.DeserializeObject<FlatPerson>(flat.LeadProperties);

                        personObj.Owner = new Lookup { Id = appintmentConfig.CreatedBy.Id };
                        //workingContext.TenantId = appintmentConfig.Tenant;
                        //workingContext.UserId = appintmentConfig.CreatedBy.UserId.ToString();

                        var personResult = personService.Value.SaveViaFlatWithResult<FlatPerson>(personObj) as Result<Guid>;

                        bussinesObj.Id = personResult.Value;
                        tempLead.Type = BusinessObjectType.Person;
                    }
                    else
                    {
                        bussinesObj.Id = tempLead.Id.Value;
                    }
                }

                var referenceDefine = new ReferenceDefine
                {
                    ReferenceName = $"{bussinesObj.FirstName} {bussinesObj.LastName}",
                    BusinessObjectType = !tempLead.Type.HasValue ? BusinessObjectType.Lead : tempLead.Type.Value,
                    TypeName = tempLead.Type == null ? (int)BusinessObjectType.Lead : (int)tempLead.Type.Value,
                    ReferenceId = bussinesObj.Id
                };

                appointment.Attendees.Add(referenceDefine);
                appointment.Location = appintmentConfig.Location;
                appointment.Description = appintmentConfig.Description;
                appointment.NotifyByEmail = true;
                appointment.NotifyBySMS = true;

                var resultMeeting = SaveViaFlatWithResult<Appointment>(appointment) as Result<Guid>;
                if (resultMeeting.IsFailure)
                    return resultMeeting;

                appointmenId = resultMeeting.Value;

                DateTime dateTime = DateTime.ParseExact(flat.MeetingBookedTime,
                                    "hh:mm tt", CultureInfo.InvariantCulture);
                TimeSpan span = dateTime.TimeOfDay;

                var identityInfo = customerService.GetById(appintmentConfig.CreatedBy.UserId);
                appintmentConfig.CreatedBy.Name = $" {identityInfo.FirstName} {identityInfo.LastName}";
                appintmentConfig.CreatedBy.Email = identityInfo.Email;
                appintmentConfig.CreatedBy.Phone = PhoneNumberHelper.GetE164(identityInfo.WorkPhoneNumber);

                var phonenumber = appintmentConfig.CreatedBy.Phone;

                if (phonenumber != null && !string.IsNullOrEmpty(phonenumber))
                {
                    var smsBody = GenerateSMSBody(tenantInfo, appintmentConfig.CreatedBy, appointment, true);
                    SmsService.Value.SendSystemSMS(to: phonenumber,
                                             body: smsBody, tenant: workingContext.TenantId?.ToString());
                }
            }
            catch (AppWebException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    return Result.Fail<Guid>(ex.Message);
            }

            return Result.Ok(appointmenId);
        }

        private void SendEmailToRecipaint(FlatAppointment flat, TenantDetailsModel companyInfo,
            TimeSpan startTimeSpan,
            FlatLead flatLead,
            string redirectURL,
            UserInfo userInfo,
            Guid tenant,
            bool byICF, bool isSystem = false, bool appAppointment = false, bool forApproveAppointment = false,
            EmailTemplateType? emailTemplateType = null, Dictionary<string, string> objectVariableList = null, string customeEmailBody = null)
        {
            EmailTemplate emailTemplate = null;

            string subjectTitle = null;
            if (emailTemplateType != null)
            {
                emailTemplate = EmailTemplateRepository.Value.GetEmailTemplateByTemplateType(emailTemplateType.Value);
            }
            else if (appAppointment || (!appAppointment && byICF))
            {
                if (forApproveAppointment)
                {
                    emailTemplate = EmailTemplateRepository.Value.GetEmailTemplateByTemplateType(EmailTemplateType.ApproveAppointmentForClient);
                }
                else
                {
                    if (objectVariableList != null && objectVariableList.Any() && objectVariableList.ContainsKey("EmailTemplateType"))//objectVariableList has value only if its in edit mode and has change 
                    {
                        if (objectVariableList["EmailTemplateType"] == EmailTemplateType.UpdateAppointment.ToString())
                        {
                            emailTemplate = EmailTemplateRepository.Value.GetEmailTemplateByTemplateType(EmailTemplateType.UpdateAppointment);
                            string ownerEmail = (objectVariableList.ContainsKey("Owner_Email_Value") ? (objectVariableList["Owner_Email_Value"]) : (userInfo.Email));
                            subjectTitle = $"Updated appointment:{flat.Name} @{flat.StartDateTime.ToLongDateString()} ({ownerEmail}) ";
                        }
                        else if (objectVariableList["EmailTemplateType"] == EmailTemplateType.Appointment.ToString())
                        {
                            emailTemplate = EmailTemplateRepository.Value.GetEmailTemplateByTemplateType(EmailTemplateType.Appointment);
                        }
                    }
                    else
                    {
                        emailTemplate = EmailTemplateRepository.Value.GetEmailTemplateByTemplateType(EmailTemplateType.Appointment);
                    }
                }
            }
            else if (!appAppointment && !byICF)
            {
                if (forApproveAppointment)
                {
                    emailTemplate = EmailTemplateRepository.Value.GetEmailTemplateByTemplateType(EmailTemplateType.ApproveAppointmentForOwner);
                }
                else
                {
                    emailTemplate = EmailTemplateRepository.Value.GetEmailTemplateByTemplateType(EmailTemplateType.BookAppointmentForOwner);
                }
            }

            if (emailTemplate == null)
                return;

            var setBody = SetPropertiesToBody(
                emailTemplate.Body,
                companyInfo,
                flat.StartDateTime.Date.Add(startTimeSpan),
                flat.Duration,
                userInfo,
                flatLead.Name,
                flatLead.Communication.Email,
                flat.Id,
                redirectURL,
                flat.TimeZone, flat.Location, flat.Description, flat.CancelToken, objectVariableList);


            EmailService.Value.SendAppointmentEmail(flat,
            byICF ? flatLead.Communication.Email : userInfo.Email,
            createdBy: userInfo.Name,
            tenant: tenant,
            customeEmailBody == null ? setBody : customeEmailBody,
            byICF, isSystem, appAppointment, subjectTitle
            );

        }

        private void SendEmailToRecipaintForAppointment(FlatAppointment flat, TenantDetailsModel companyInfo,
           TimeSpan startTimeSpan,
           FlatLead flatLead,
           string redirectURL,
           UserInfo userInfo,
           Guid tenant, bool isSystem = false, bool appAppointment = false, Dictionary<string, string> objectVariableList = null)
        {
            EmailTemplate emailTemplate = null;
            bool byICF = false;
            string subjectTitle = null;
            var appointmentHolder = userInfo.Email;
            if (appAppointment || !isSystem)
            {

                byICF = true;
                try
                {
                    objectVariableList["{titleEmail}"] = objectVariableList["Owner_Email_Value"];
                    objectVariableList["{titleName}"] = objectVariableList["Owner_Name_Value"];
                }
                catch (Exception ex)
                {
                    logger.InfoException("Error of objectVariableList ", ex);

                }
                switch (flat.State)
                {

                    case AppointmentState.Approve:
                        emailTemplate = EmailTemplateRepository.Value.GetEmailTemplateByTemplateType(EmailTemplateType.AppointmentWithState);
                        subjectTitle = $"Accepted:{flat.Name} @{flat.StartDateTime.ToLongDateString()} ({objectVariableList["Owner_Email_Value"]}) ";
                        break;
                    case AppointmentState.Pending:
                        emailTemplate = EmailTemplateRepository.Value.GetEmailTemplateByTemplateType(EmailTemplateType.ApproveAppointmentForClient);
                        break;
                    case AppointmentState.Reject:
                        emailTemplate = EmailTemplateRepository.Value.GetEmailTemplateByTemplateType(EmailTemplateType.AppointmentWithState);
                        subjectTitle = $"Declined:{flat.Name} @{flat.StartDateTime.ToLongDateString()} ({objectVariableList["Owner_Email_Value"]}) ";
                        break;
                    case AppointmentState.Cancel:
                        emailTemplate = EmailTemplateRepository.Value.GetEmailTemplateByTemplateType(EmailTemplateType.CancelAppointmentForClient);
                        subjectTitle = $"Canceled:{flat.Name} @{flat.StartDateTime.ToLongDateString()} ({objectVariableList["Owner_Email_Value"]}) ";
                        break;
                    default:
                        emailTemplate = EmailTemplateRepository.Value.GetEmailTemplateByTemplateType(EmailTemplateType.ApproveAppointmentForClient);
                        break;
                }
            }
            else
            {

                try
                {
                    objectVariableList["{titleEmail}"] = objectVariableList["User_Email_Value"];
                    objectVariableList["{titleName}"] = objectVariableList["User_Name_Value"];
                    appointmentHolder = objectVariableList["Owner_Email_Value"];
                }
                catch (Exception ex)
                {

                    logger.InfoException("Error of objectVariableList ", ex);
                }
                switch (flat.State)
                {
                    case AppointmentState.Approve:
                        emailTemplate = EmailTemplateRepository.Value.GetEmailTemplateByTemplateType(EmailTemplateType.AppointmentWithState);
                        subjectTitle = $"Accepted:{flat.Name} @{flat.StartDateTime.ToLongDateString()} ({flatLead.Communication.Email}) ";
                        break;
                    case AppointmentState.Pending:
                        emailTemplate = EmailTemplateRepository.Value.GetEmailTemplateByTemplateType(EmailTemplateType.ApproveAppointmentForOwner);
                        break;
                    case AppointmentState.Reject:
                        emailTemplate = EmailTemplateRepository.Value.GetEmailTemplateByTemplateType(EmailTemplateType.AppointmentWithState);
                        subjectTitle = $"Declined:{flat.Name} @{flat.StartDateTime.ToLongDateString()} ({flatLead.Communication.Email}) ";
                        break;
                    case AppointmentState.Cancel:
                        emailTemplate = EmailTemplateRepository.Value.GetEmailTemplateByTemplateType(EmailTemplateType.CancelAppointmentForOwner);
                        subjectTitle = $"Canceled:{flat.Name} @{flat.StartDateTime.ToLongDateString()} ({flatLead.Communication.Email}) ";
                        break;
                    default:
                        emailTemplate = EmailTemplateRepository.Value.GetEmailTemplateByTemplateType(EmailTemplateType.ApproveAppointmentForOwner);
                        break;
                }
            }

            if (emailTemplate == null)
                return;


            var setBody = SetPropertiesToBody(
                emailTemplate.Body,
                companyInfo,
                flat.StartDateTime.Date.Add(startTimeSpan),
                flat.Duration,
                userInfo,
                flatLead.Name,
                flatLead.Communication.Email,
                flat.Id,
                redirectURL,
                flat.TimeZone, flat.Location, flat.Description, flat.CancelToken, objectVariableList: objectVariableList);



            EmailService.Value.SendAppointmentEmail(flat,
            isSystem ? appointmentHolder : flatLead.Communication.Email,
            createdBy: userInfo.Name,
            tenant: tenant,
            setBody,
            byICF, isSystem, appAppointment, subjectTitle
            );



        }

        private string SetPropertiesToCustomeBody(string incomebody, Appointment appointment, FlatAppointment flat, FlatLead contact, TenantDetailsModel companyInfo, Dictionary<string, string> objectVariableList)
        {
            var body = incomebody;
            string ownerName = "";
            TimeSpan duration;//?? appointment.Duration;
            DateTime? StartDateTime;
            try
            {
                if (flat == null)
                {
                    duration = appointment.Duration;
                    ownerName = appointment.Owner?.Name;
                    StartDateTime = appointment.StartDateTime;

                }
                else
                {
                    ownerName = (flat?.Owner?.Value?.ToString()) ?? (appointment.Owner?.Name);
                    duration = flat.Duration;//?? appointment.Duration;
                    StartDateTime = flat.StartDateTime;

                }
                body = body.Replace("{Contact Name}", contact.Name)
                        .Replace("{Contact Email}", contact.Communication?.Email)
                        //.Replace("{no_reply_description}", "")
                        .Replace("{Appointment Name}", flat?.Name ?? appointment?.Name)
                        .Replace("{Meeting Head Line}", appointment.AppointmentConfig == null ? string.Empty : appointment.AppointmentConfig?.MeetingHeadLine)
                        .Replace("{Appointment Creation Date}", appointment.CreationDate.ToLongDateString())
                        .Replace("{Appointment Owner}", ownerName)
                        .Replace("{Appointment Location}", flat?.Location ?? appointment?.Location)
                        .Replace("{Appointment Description}", flat?.Description ?? appointment?.Description)
                        .Replace("{Appointment Duration}", $"{duration.Hours:D2}:{duration.Minutes:D2}")
                        .Replace("{Appointment Start Date}", StartDateTime?.ToLongDateString())
                        .Replace("{logo}", string.IsNullOrEmpty(companyInfo.Logo) ? string.Empty : companyInfo.Logo.ToString())
                        .Replace("{companyLogo}", string.IsNullOrEmpty(companyInfo.Logo) ? string.Empty : companyInfo.Logo.ToString())
                            ;
            }
            catch (Exception e)
            {

            }
            return body;
        }
        private string SetPropertiesToBody(
            string body,
            TenantDetailsModel companyInfo,
            DateTime appointmentStartDateTime,
            TimeSpan duration,
            UserInfo owner,
            string recipiantName,
            string recipiantEmail,
            Guid appointmentId, string redirectURL, string timezone, string location, string description, string cancelToken, Dictionary<string, string> objectVariableList = null)
        {
            try
            {
                string ownerName = owner.Name;
                string ownerEmail = owner.Email;
                string ownerPhone = owner.Phone;
                string link = BaseAddress.EnsureTrailingSlash() + $"api/appointment/cancel/{appointmentId}?cancelToken={cancelToken}&tenant={workingContext.TenantId}";

                if (objectVariableList != null && objectVariableList.Any())
                {
                    if (objectVariableList.ContainsKey("Owner_Name_Value"))
                        objectVariableList.TryGetValue("Owner_Name_Value", out ownerName);

                    if (objectVariableList.ContainsKey("Owner_Email_Value"))
                        objectVariableList.TryGetValue("Owner_Email_Value", out ownerEmail);

                    if (objectVariableList.ContainsKey("Owner_Phone_Value"))
                        objectVariableList.TryGetValue("Owner_Phone_Value", out ownerPhone);
                    objectVariableList.ForEach(variable =>
                    {
                        if (!string.IsNullOrEmpty(variable.Key) && variable.Key.StartsWith("{") && variable.Key.EndsWith("}"))
                            body = body.Replace(variable.Key, variable.Value);
                    });
                    //currentState
                }
                body = body.Replace("{OwnerName}", ownerName)
                    .Replace("{ownerName}", ownerName)
                    .Replace("{customerLogo}", string.IsNullOrEmpty(companyInfo.Logo) ? string.Empty : companyInfo.Logo.ToString())
                    .Replace("{companyLogo}", string.IsNullOrEmpty(companyInfo.Logo) ? string.Empty : companyInfo.Logo.ToString())
                    .Replace("{logo}", string.IsNullOrEmpty(companyInfo.Logo) ? string.Empty : companyInfo.Logo.ToString())
                    .Replace("{tell}", companyInfo.Phone)
                    .Replace("{timeZoneType}", string.IsNullOrEmpty(timezone) ? "none" : "block")
                    .Replace("{displayAddress}", string.IsNullOrEmpty(location) ? "none" : "block")
                    .Replace("{displayNote}", string.IsNullOrEmpty(description) ? "none" : "block")
                    .Replace("{timezone}", string.IsNullOrEmpty(timezone) ? string.Empty : timezone)
                    .Replace("{appointmentId}", appointmentId.ToString())
                    .Replace("{location}", string.IsNullOrEmpty(location) ? string.Empty : location.ToString())
                    .Replace("{companyName}", string.IsNullOrEmpty(companyInfo.Name) ? string.Empty : companyInfo.Name.ToString())
                    .Replace("{adressCompany}", $"{companyInfo.CompanyAddress} {companyInfo.CompanyAddress2} {companyInfo.City} {companyInfo.State} {companyInfo.Zip} {companyInfo.Country}")
                    .Replace("{websiteCompany}", string.IsNullOrEmpty(companyInfo.CompanyDomain) ? string.Empty : companyInfo.CompanyDomain.ToString())
                    .Replace("{dearname}", $"{recipiantName}".ToString())
                    .Replace("{clientName}", $"{recipiantName}".ToString())
                    .Replace("{RedirectURL}", redirectURL)
                    .Replace("{Img}", ownerName)
                    .Replace("{ownerEmail}", ownerEmail)
                    .Replace("{ownerPhone}", ownerPhone)
                    .Replace("{description}", description)
                    .Replace("{note}", description)
                    .Replace("{cancelHref}", link)
                    .Replace("{fullDate}", appointmentStartDateTime.ToLongDateString())
                    .Replace("{Day}", appointmentStartDateTime.ToLongDateString().Split(',')[1].Trim().Split(' ')[1])
                    .Replace("{day}", appointmentStartDateTime.ToLongDateString().Split(',')[1].Trim().Split(' ')[1])
                    .Replace("{Month}", appointmentStartDateTime.ToLongDateString().Split(',')[1].Trim().Split(' ')[0])
                    .Replace("{month}", appointmentStartDateTime.ToLongDateString().Split(',')[1].Trim().Split(' ')[0])
                    .Replace("{time}", $"{appointmentStartDateTime.Hour:D2}:{appointmentStartDateTime.Minute:D2} " +
                                       $"{(appointmentStartDateTime.Hour > 12 ? "PM" : "AM")}")
                    .Replace("{Duration}", $"{duration.Hours:D2}:{duration.Minutes:D2}")
                    .Replace("{duration}", $"{duration.Hours:D2}:{duration.Minutes:D2}")
                    .Replace("{email}", recipiantEmail)
                    .Replace("{Contact Name}", $"{recipiantName}".ToString())
                //.Replace("{Contact Email}" , request.Contact.Email)
                //.Replace("{Appointment Name}" , appointment.Name)
                //.Replace("{Meeting Head Line}" , appointment.AppointmentConfig == null ? string.Empty : appointment.AppointmentConfig?.MeetingHeadLine)
                //.Replace("{Appointment Creation Date}" , appointment.CreationDate.ToLongDateString())
                .Replace("{Appointment Owner}", ownerName)
                .Replace("{Appointment Location}", string.IsNullOrEmpty(location) ? string.Empty : location.ToString())
                .Replace("{Appointment Description}", description)
                .Replace("{Appointment Duration}", $"{duration.Hours:D2}:{duration.Minutes:D2}")
                .Replace("{Appointment Start Date}", appointmentStartDateTime.ToLongDateString())
                    ;
                #region empty the variables

                //var test = Regex.Replace(body, "\\{[^}]*\\}", string.Empty);
                #endregion empty the variables
            }
            catch (Exception ex)
            {
                throw;
            }
            return body;
        }

        public IList<FlatEvent> GetAllAppointment(bool forCurrentUser, DateTime? fromDate)
        {
            IList<Appointment> result = new List<Appointment>();
            List<FlatEvent> flatEvents = new List<FlatEvent>();
            if (!forCurrentUser)
            {
                if (fromDate.HasValue)
                    result = appointmentRepository.GetAll().Where(c =>
                c.StartDateTime.Date >= fromDate.Value.AddDays(-1) && c.StartDateTime.Date <= fromDate.Value.AddMonths(1).AddDays(12)).ToList();
                else
                    result = appointmentRepository.GetAll().ToList();
            }
            else
            {
                var currentUserId = Guid.Parse(workingContext.UserId);
                result = appointmentRepository.GetAll().Where(c =>
                c.CreatedBy.UserId == currentUserId &&
                c.StartDateTime.Date == DateTime.UtcNow.Date).ToList();
            }

            var meetings = result.Select(s => new FlatEvent
            {
                Name = s.Name,
                AppointmentColorTypeId = s.AppointmentColorTypeId,
                Id = s.Id.ToString(),
                StartDateTime = s.StartDateTime,
                Color = string.IsNullOrEmpty(s.Color) ? "#994e88" : s.Color,
                Duration = s.Duration,
                CRM_Meetingt = true,
                State = (int)s.State,
                Owner = new Lookup { Id = s.CreatedBy.UserId, Value = s.CreatedBy.Name },
            }).ToList();
            flatEvents.AddRange(meetings);

            try
            {
                if (!forCurrentUser)
                {
                    var externalProviderAccounts = GetAccountsByServiceType(workingContext.TenantId.ToString(), workingContext.UserId);
                    logger.InfoFormat("externalProviderAccounts : {0}", externalProviderAccounts.Any() ? externalProviderAccounts.FirstOrDefault().ClientId : "It's empty");

                    List<ExternalProvider> providers = externalProviderAccounts?.GroupBy(q => q.Provider).Select(q => q.Key).ToList();
                    if (providers != null && providers.Any())
                    {
                        foreach (var provider in providers)
                        {
                            if (provider == ExternalProvider.None)
                                continue;
                            var calendarEvent = CalendarFactory.Create(provider).GetCalendar(externalProviderAccounts.Where(c => c.Provider == provider), result.Select(c => c.CalendarId).ToList(), fromDate);
                            if (calendarEvent != null && calendarEvent.Value != null)
                                flatEvents.AddRange(calendarEvent.Value.ToList());
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                logger.InfoException("Error get calendars ", ex);
            }

            return flatEvents;
        }
        public List<FlatEvent> GetSpecificAppointments(Guid OwnerId, DateTime fromDate, TimeSpan duration, Guid currentID)
        {
            IList<Appointment> result = new List<Appointment>();

            var appointments = appointmentRepository.GetAll().Where(c =>
            c.Id != currentID &&
               c.OwnerId == OwnerId &&
               c.State != AppointmentState.Reject &&
               c.State != AppointmentState.Cancel &&
               (c.StartDateTime.Date == fromDate.Date ||
               c.StartDateTime.Date == fromDate.Date.AddDays(-1) ||
               c.StartDateTime.Date == fromDate.Date.AddDays(1))
               ).Select(it => new
               {
                   it.StartDateTime,
                   it.Duration,
                   it.StartDateTimeSpan,
                   it.State
               }).ToList();

            var hasAnyConflict = appointments.Where(
                 s =>
                 (duration != TimeSpan.Zero && s.Duration != TimeSpan.Zero) &&
                 ((s.StartDateTime == fromDate) ||
                 (s.StartDateTime > fromDate && (s.StartDateTime.Subtract(fromDate) < duration)) ||
                 (s.StartDateTime < fromDate && (fromDate.Subtract(s.StartDateTime) < s.Duration)))
                 ).Select(it => new FlatEvent
                 {
                     Duration = it.Duration,
                     StartDateTime = it.StartDateTime,
                     State = (int)it.State,

                 }).ToList();
            return hasAnyConflict;
        }

        public Result<IList<FlatEvent>> GetAllBoockedAppointments(UserInfo userInfo, DateTime? fromDate = null)
        {
            IList<FlatEvent> meetings = new List<FlatEvent>();

            try
            {
                var result = appointmentRepository.GetAllByTenantId(userInfo.Tenant.Value).Where(c => c.CreatedBy.Id == userInfo.Id).ToList();
                meetings = result.Select(s => new FlatEvent
                {
                    Name = s.Name,
                    Id = s.Id.ToString(),
                    StartDateTime = s.StartDateTime,
                    Duration = s.Duration,
                    CRM_Meetingt = true,
                    State = (int)s.State,
                    Owner = new Lookup { Id = s.CreatedBy.UserId, Value = s.CreatedBy.Name },
                }).ToList();

                ICalendarService CalendarService = new GoogleCalendar();

                var externalProviderAccounts = GetAccountsByServiceType(userInfo.Tenant.Value.ToString(), userInfo.UserId.ToString());
                var list = CalendarService.GetCalendar(externalProviderAccounts.Where(c => c.OwnerId == userInfo.UserId), result.Select(c => c.CalendarId).ToList(), fromDate);
                meetings.ForEach(c => list.Value.Add(c));
                return list;
            }
            catch (Exception ex)
            {
                logger.InfoException("Don't sync calendar with any provider: {0}", ex);
                return Result.Ok<IList<FlatEvent>>(meetings);
            }
        }

        private FlatLead GetEmailAttendees(BusinessObjectType BusinessObjectType, Guid ReferenceId) => GetEmailAttendees1(BusinessObjectType, ReferenceId);
        private FlatLead GetEmailAttendees1(BusinessObjectType BusinessObjectType, Guid ReferenceId)
        {
            switch (BusinessObjectType)
            {
                case BusinessObjectType.Lead:
                    {
                        var lead = LeadRepository.Value.Get(ReferenceId);
                        return new FlatLead
                        {
                            Id = lead.Id,
                            Name = lead.FirstName + " " + lead.LastName,
                            Communication = new FlatCommunication
                            {
                                Email = lead.Communication.Email,
                                Phone = lead.Communication.GetMobilePhone(),
                                FlatPhoneNumbers = lead.Communication.LeadPhoneNumbers.Select(c => new FlatPhoneNumber { Number = c.NumberE164, PhoneType = c.PhoneType }).ToList()
                            }
                        };
                    }
                case BusinessObjectType.Company:
                    {
                        var company = CompanyRepository.Value.Get(ReferenceId);
                        return new FlatLead
                        {
                            Id = company.Id,
                            Name = company.Name,
                            Communication = new FlatCommunication
                            {
                                Email = company.Communication.Email,
                                FlatPhoneNumbers = company.Communication.CompanyPhoneNumbers.Select(c => new FlatPhoneNumber { Number = c.NumberE164, PhoneType = c.PhoneType }).ToList()
                            }
                        };
                    }
                case BusinessObjectType.Person:
                    {
                        var person = PersonRepository.Value.Get(ReferenceId);
                        return new FlatLead
                        {
                            Id = person.Id,
                            Name = person.FirstName + " " + person.LastName,
                            Communication = new FlatCommunication
                            {
                                Email = person.Communication.Email,
                                FlatPhoneNumbers = person.Communication.PersonPhoneNumbers.Select(c => new FlatPhoneNumber { Number = c.NumberE164, PhoneType = c.PhoneType }).ToList()
                            }
                        };
                    }

                case BusinessObjectType.Opportunity:
                    {
                        var associateContact = OpportunityRepository.Value.Get(ReferenceId).AssociatedContacts
                            .SingleOrDefault(c => c.IsPrimary);

                        if (associateContact != null && associateContact.CompanyId != null)
                        {
                            return new FlatLead
                            {
                                Id = associateContact.Company.Id,
                                Name = associateContact.Company.Name,
                                Communication = new FlatCommunication { Email = associateContact.Company.Communication.Email }
                            };
                        }

                        if (associateContact != null && associateContact.PersonId != null)
                        {
                            return new FlatLead
                            {
                                Id = associateContact.PersonId.Value,
                                Name = associateContact.Person.Name,
                                Communication = new FlatCommunication { Email = associateContact.Person.Communication.Email }
                            };
                        }

                        return null;
                    }

                case BusinessObjectType.Ticket:
                    {
                        var ticket = TicketRepository.Value.Get(ReferenceId);
                        var associateContact = ticket.TicketBusinessObject
                            .SingleOrDefault(c => c.IsPrimary);

                        if (associateContact != null && associateContact.CompanyId != null)
                        {
                            return new FlatLead
                            {
                                Id = associateContact.Company.Id,
                                Name = associateContact.Company.Name,
                                Communication = new FlatCommunication { Email = associateContact.Company.Communication.Email }
                            };
                        }

                        if (associateContact != null && associateContact.PersonId != null)
                        {
                            return new FlatLead
                            {
                                Id = associateContact.PersonId.Value,
                                Name = associateContact.Person.Name,
                                Communication = new FlatCommunication { Email = associateContact.Person.Communication.Email }
                            };
                        }

                        return null;
                    }

                case BusinessObjectType.Project:
                    {
                        var project = ProjectRepository.Value.Get(ReferenceId);

                        if (project != null && project.CustomerId != null)
                        {
                            if (project.Customer == BusinessObjectType.Person)
                            {
                                var person = PersonRepository.Value.Get(project.CustomerId);

                                return new FlatLead
                                {
                                    Id = person.Id,
                                    Name = person.FirstName + " " + person.LastName,
                                    Communication = new FlatCommunication { Email = person.Communication.Email }
                                };
                            }
                            else
                            {
                                var company = CompanyRepository.Value.Get(project.CustomerId);

                                return new FlatLead
                                {
                                    Id = company.Id,
                                    Name = company.Name,
                                    Communication = new FlatCommunication { Email = company.Communication.Email }
                                };
                            }
                        }

                        return null;
                    }

                default: return null;
            }
        }

        public IEnumerable<ExternalProviderAccounts> GetAccountsByServiceType(string tenant, string userId, bool forCalendar = true)
        {
            var notificationBaseUrl = ConfigurationManager.AppSettings["NotificationBaseUrl"];
            if (string.IsNullOrEmpty(notificationBaseUrl))
                throw new Exception("Notification server url not configure.");

            ExternalProviderService externalProviderService = new ExternalProviderService(notificationBaseUrl, this._identityOptions);
            IEnumerable<ExternalProviderAccounts> externalProviderAccounts = new List<ExternalProviderAccounts>();

            try
            {
                if (forCalendar)
                    externalProviderAccounts = externalProviderService.GetCalendars(tenant, subjectId: Guid.Parse(userId)).Where(c => !c.Expired);
                else
                    externalProviderAccounts = externalProviderService.GetEmailAccounts(tenant, subjectId: Guid.Parse(userId)).Where(c => !c.Expired);
            }
            catch (Exception e)
            {
                return externalProviderAccounts;
            }

            return externalProviderAccounts;
        }

        private Result UpdateState(Appointment appointment)
        {
            try
            {
                var RedirectUrl = ConfigurationManager.AppSettings["RedirectUrl"];

                var businessLogoUrl = $"{RedirectUrl}api/businessLogo/{workingContext.TenantId.Value}";

                TenantDetailsModel tenantInfo = null;
                var tenantId = workingContext.TenantId;
                tenantInfo = TenantHttpClient.Value.GetTenantDetail(tenantId);
                tenantInfo.Logo = businessLogoUrl;

                UserInfo currentUser = null;
                if (appointment.AppointmentConfigId.HasValue)
                    currentUser = appointmentConfigRepository.Value.Get(appointment.AppointmentConfigId.Value).CreatedBy;
                else
                    currentUser = UtilityService.Value.GetCurrentUser().Value;

                var identityInfo = customerService.GetById(currentUser.UserId);
                currentUser.Name = $" {identityInfo.FirstName} {identityInfo.LastName}";
                currentUser.Email = identityInfo.Email;
                currentUser.Phone = PhoneNumberHelper.GetE164(identityInfo.WorkPhoneNumber);

                var Attendees = TotalActivityService.Value.GetAllActivityForActivity(ActivityType.Appointment, appointment.Id);
                DateTime dateTime = DateTime.ParseExact(appointment.StartDateTimeSpan,
                                 "hh:mm tt", CultureInfo.InvariantCulture);
                TimeSpan span = dateTime.TimeOfDay;
                if (appointment.NotifyByEmail || appointment.NotifyBySMS)
                {
                    foreach (var item in Attendees)
                    {
                        var flatLead = GetEmailAttendees(item.BusinessObjectType, item.ReferenceId);

                        if (appointment.NotifyBySMS)
                        {
                            if (flatLead.Communication.Phone != null && PhoneNumberHelper.GetE164(flatLead.Communication.Phone) != null)
                            {
                                var smsBodyCustomer = GenerateBodyExtension.SmsBodyWithoutApproveCustomer(
                                                                            tenantInfo,
                                                                            appointment.AppointmentConfig.CreatedBy,
                                                                            new FlatAppointment
                                                                            {
                                                                                StartDateTime = appointment.StartDateTime.Date.Add(span),
                                                                                StartDateTimeSpan = appointment.StartDateTimeSpan,
                                                                                Attendees = new List<ReferenceDefine> { new ReferenceDefine { ReferenceName = flatLead.Name } }
                                                                            });
                                SmsService.Value.SendSystemSMS(to: flatLead.Communication.Phone,
                                                                 body: smsBodyCustomer, tenant: workingContext.TenantId?.ToString());
                            }
                            var createdBy = customerService.GetById(appointment.AppointmentConfig.CreatedBy.Id);
                            if (createdBy != null && createdBy.WorkPhoneNumber != null && PhoneNumberHelper.GetE164(createdBy.WorkPhoneNumber) != null)
                            {
                                var smsBodyOwner = GenerateBodyExtension.SmsBodyWithoutApproveOwner(
                                                           tenantInfo,
                                                           appointment.AppointmentConfig.CreatedBy,
                                                           new FlatAppointment
                                                           {
                                                               StartDateTime = appointment.StartDateTime.Date.Add(span),
                                                               StartDateTimeSpan = appointment.StartDateTimeSpan,
                                                               Attendees = new List<ReferenceDefine> { new ReferenceDefine { ReferenceName = flatLead.Name } }
                                                           });

                                SmsService.Value.SendSystemSMS(to: createdBy.WorkPhoneNumber,
                                                                 body: smsBodyOwner, tenant: workingContext.TenantId?.ToString());
                            }
                        }

                        if (appointment.NotifyByEmail)
                        {
                            NotifyByEmailToRecipaint(new FlatAppointment
                            {
                                Id = appointment.Id,
                                Location = appointment.Location,
                                Description = appointment.Description,
                                Duration = appointment.Duration,
                                TimeZone = appointment.TimeZone,
                                StartDateTime = appointment.StartDateTime,
                                StartDateTimeSpan = appointment.StartDateTimeSpan,
                            }, currentUser, tenantInfo, tenantId, true, span, flatLead,
                            emailTemplateType: EmailTemplateType.ApproveAppointmentForClient);

                            NotifyByEmailToRecipaint(new FlatAppointment
                            {
                                Id = appointment.Id,
                                Location = appointment.Location,
                                Description = appointment.Description,
                                Duration = appointment.Duration,
                                TimeZone = appointment.TimeZone,
                                StartDateTime = appointment.StartDateTime,
                                StartDateTimeSpan = appointment.StartDateTimeSpan,
                            }, currentUser, tenantInfo, tenantId, false, span, flatLead, emailTemplateType: EmailTemplateType.ApproveAppointmentForOwner);
                        }
                    }
                }
                return Result.Ok();
            }
            catch (Exception e)
            {
                return Result.Fail(e.Message);
            }
        }

        public Result<Guid> SignContract(FlatSignContract flat)
        {
            var contract = contractRepository.Value.Get(flat.ContractId);

            if (contract == null)
                return Result.Fail<Guid>("There is not exist contract.");
            var appointmentContract = new AppointmentContract
            {
                ContractId = contract.Id,
                Contract = contract,
                DroppedItems = flat.DroppedItems != null ? JsonConvert.SerializeObject(flat.DroppedItems) : null,
                EditorBody = flat.EditorBody
            };
            try
            {
                contract.AppointmentContract.Add(appointmentContract);
                contractRepository.Value.Update(contract);
            }
            catch (Exception e)
            {
                return Result.Fail<Guid>(e.Message);
            }

            return Result.Ok(appointmentContract.Id);
        }
        private string RandomString(int length)
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public Result<Guid> AnswerToQuestionary(FlatAnswerToQuestionary flat)
        {
            var questionary = questionaryRepository.Value.Get(flat.QuestionaryId);

            if (questionary == null)
                return Result.Fail<Guid>("There is not exist questionary.");
            var appointmentContract = new AppointmentQuestionary
            {
                QuestionaryId = questionary.Id,
                Questionary = questionary,
                CustomProperties = !string.IsNullOrEmpty(flat.Pages) ? flat.Pages : JsonConvert.SerializeObject(flat.CustomProperties)
            };

            try
            {
                questionary.AppointmentQuestionary.Add(appointmentContract);
                questionaryRepository.Value.Update(questionary);
            }
            catch (Exception e)
            {
                return Result.Fail<Guid>(e.Message);
            }

            return Result.Ok(appointmentContract.Id);
        }

        public Result RegisterAppointment(List<FlatRegisterAppointment> model)
        {
            //var appointmentId = model.FirstOrDefault(x => x.Key == "appointment")?.Value


            return Result.Ok();
        }

        public Result<Guid> ConfirmAppointment(List<FlatConfirmAppointment> Dto)
        {

            var appointmentId = Dto.FirstOrDefault(x => x.Key.ToLower() == "appointment");

            if (appointmentId == null)
            {
                return Result.Fail<Guid>("There is not exist appointment.");
            }
            var appointment = appointmentRepository.Get(Guid.Parse(appointmentId.Value));

            if (appointment == null)
            {
                return Result.Fail<Guid>("There is not exist appointment.");
            }

            var appointmentActivity = totalActivityRepository.Value.Get(x => x.ActivityType == ActivityType.Appointment && x.ActivityId == Guid.Parse(appointmentId.Value) && x.BusinessObjectType == BusinessObjectType.Person).FirstOrDefault();

            if (appointmentActivity == null)
                return Result.Fail<Guid>("There is not exist appointmentActivity.");


            var personId = appointmentActivity.BusinessTypeObjectId;

            var person = PersonRepository.Value.Get(personId);

            if (person == null)
                return Result.Fail<Guid>("There is not exist person.");

            var appointmentContractIds = Dto.Where(x => x.Key == "contract").ToList();


            foreach (var appointmentContractId in appointmentContractIds)
            {
                var appointmentContract = appointmentContractRepository.Value.Get(Guid.Parse(appointmentContractId.Value));
                var contract = contractRepository.Value.Get(appointmentContract.ContractId);

                if (contract == null)
                    return Result.Fail<Guid>("There is not exist contract.");

                var name = "\"id\":\"{ContactName}\"";
                var email = "\"id\":\"{ContactEmail}\"";

                var index = appointmentContract.DroppedItems.IndexOf(name);

                if (index > -1)
                {
                    appointmentContract.DroppedItems = appointmentContract.DroppedItems.Insert(index + name.Length, $",\"value\":\"{person.FirstName + " " + person.LastName}\"");
                }

                index = appointmentContract.DroppedItems.IndexOf(email);

                if (index > -1)
                {
                    appointmentContract.DroppedItems = appointmentContract.DroppedItems.Insert(index + email.Length, $",\"value\":\"{person.GetEmail()}\"");
                }


                person.PersonContracts.Add(new PersonContract
                {
                    Person = person,
                    PersonId = personId,
                    Contract = contract,
                    ContractId = contract.Id,
                    DroppedItems = appointmentContract.DroppedItems,
                    EditorBody = appointmentContract.EditorBody,
                    Status = ContractStatus.Signed,
                    SignDate = DateTime.UtcNow,
                });
                PersonRepository.Value.Update(person);
            }

            var appointmentQuestionaryIds = Dto.Where(x => x.Key == "questionary").ToList();

            foreach (var appointmentQuestionaryId in appointmentQuestionaryIds)
            {
                var appointmentQuestionary = appointmentQuestionaryRepository.Value.Get(Guid.Parse(appointmentQuestionaryId.Value));

                var questionary = questionaryRepository.Value.Get(appointmentQuestionary.QuestionaryId);


                if (questionary == null)
                    return Result.Fail<Guid>("There is not exist questionary.");

                person.PersonQuesionaries.Add(new PersonQuesionary
                {
                    PersonId = personId,
                    Person = person,
                    Questionary = questionary,
                    QuestionaryId = questionary.Id,
                    Status = QuestionaryStatus.Completed,
                    CompleteDate = DateTime.UtcNow,
                    CustomProperties = appointmentQuestionary.CustomProperties
                });

                PersonRepository.Value.Update(person);
            }

            appointment.WorkFlow = JsonConvert.SerializeObject(Dto);
            appointmentRepository.Update(appointment);



            try
            {
                if (appointment?.AppointmentConfig?.CreatedBy?.Email != null && appointment.AppointmentConfig.ApproveRequired)
                {
                    TenantDetailsModel tenantInfo = null;
                    var tenantId = appointment.AppointmentConfig.Tenant;
                    tenantInfo = TenantHttpClient.Value.GetTenantDetail(tenantId);
                    DateTime dateTime = DateTime.ParseExact(appointment.StartDateTimeSpan,
                               "hh:mm tt", CultureInfo.InvariantCulture);
                    TimeSpan span = dateTime.TimeOfDay;

                    var identityInfo = customerService.GetById(appointment.AppointmentConfig.CreatedBy.UserId);
                    appointment.AppointmentConfig.CreatedBy.Name = $" {identityInfo.FirstName} {identityInfo.LastName}";
                    appointment.AppointmentConfig.CreatedBy.Email = identityInfo.Email;
                    appointment.AppointmentConfig.CreatedBy.Phone = PhoneNumberHelper.GetE164(identityInfo.WorkPhoneNumber);

                    //var ownerEmailTemplate = EmailTemplateRepository.Value.GetList().ToList().SingleOrDefault(c => c.EmailTemplateType == EmailTemplateType.ApproveAppointmentForOwner);
                    var ownerEmailTemplate = EmailTemplateRepository.Value.GetEmailTemplateByTemplateType(EmailTemplateType.ApproveAppointmentForOwner);
                    if (ownerEmailTemplate != null)
                    {

                        //var setBody = SetPropertiesToBody(ownerEmailTemplate.Body);
                        var smsBody = GenerateBodyExtension.SmsBodyApproveRequiredOwner(
                            tenantInfo,
                            appointment.AppointmentConfig.CreatedBy,
                            new FlatAppointment
                            {
                                StartDateTime = appointment.StartDateTime.Date.Add(span),
                                StartDateTimeSpan = appointment.StartDateTimeSpan,
                                Attendees = new List<ReferenceDefine> { new ReferenceDefine { ReferenceName = person.Name } }
                            });

                        SmsService.Value.SendSystemSMS(to: appointment.AppointmentConfig.CreatedBy.Phone,
                                                         body: smsBody, tenant: workingContext.TenantId?.ToString());

                        var setBody = SetPropertiesToBody(
                                           ownerEmailTemplate.Body,
                                           tenantInfo,
                                           appointment.StartDateTime.Date.Add(span),
                                           appointment.Duration,
                                           appointment.AppointmentConfig.CreatedBy,// $"{person.FirstName} {person.LastName}",
                                           person.Name,
                                           person.Communication?.Email,
                                           appointment.Id,
                                           null,
                                           appointment.TimeZone, appointment.Location, appointment.Description, appointment.CancelToken);

                        EmailService.Value.SendApproveAppointmentEmail(
                        appointment?.AppointmentConfig?.CreatedBy?.Email,
                        createdBy: appointment?.AppointmentConfig?.CreatedBy?.Name,
                        tenant: appointment.AppointmentConfig.Tenant.Value,
                        setBody
                        );

                    }

                    //var clientEmailTemplate = EmailTemplateRepository.Value.GetList().ToList().SingleOrDefault(c => c.EmailTemplateType == EmailTemplateType.ApproveAppointmentForClient);
                    var clientEmailTemplate = EmailTemplateRepository.Value.GetEmailTemplateByTemplateType(EmailTemplateType.ApproveAppointmentForClient);
                    if (clientEmailTemplate != null)
                    {
                        var smsBody = GenerateBodyExtension.SmsBodyApproveRequiredCustomer(
                            tenantInfo,
                            appointment.AppointmentConfig.CreatedBy,
                            new FlatAppointment
                            {
                                StartDateTime = appointment.StartDateTime.Date.Add(span),
                                StartDateTimeSpan = appointment.StartDateTimeSpan,
                                Attendees = new List<ReferenceDefine> { new ReferenceDefine { ReferenceName = person.Name } }
                            });

                        SmsService.Value.SendSystemSMS(to: appointment.AppointmentConfig.CreatedBy.Phone,
                                                         body: smsBody, tenant: workingContext.TenantId?.ToString());


                        var setBody = SetPropertiesToBody(
                                          clientEmailTemplate.Body,
                                          tenantInfo,
                                          appointment.StartDateTime.Date.Add(span),
                                          appointment.Duration,
                                          appointment.AppointmentConfig.CreatedBy,
                                          person.Name,
                                          person.Communication?.Email,
                                          appointment.Id,
                                          null,
                                          appointment.TimeZone, appointment.Location, appointment.Description, appointment.CancelToken);


                        EmailService.Value.SendApproveAppointmentEmail(
                        person.Communication?.Email,
                        createdBy: appointment?.AppointmentConfig?.CreatedBy?.Name,
                        tenant: appointment.AppointmentConfig.Tenant.Value,
                        setBody
                        );

                    }


                }
            }
            catch (Exception ex)
            {
                logger.InfoException("Error : ", ex);
            }

            return Result.Ok(personId);
        }

        public Result<object> GetPersonInfo(Guid appointmentId)
        {
            var appointmentActivity = totalActivityRepository.Value.Get(x => x.ActivityType == ActivityType.Appointment && x.ActivityId == appointmentId && x.BusinessObjectType == BusinessObjectType.Person).FirstOrDefault();

            if (appointmentActivity == null)
                return Result.Fail<object>("There is not exist appointmentActivity.");


            var personId = appointmentActivity.BusinessTypeObjectId;

            var person = PersonRepository.Value.Get(personId);

            if (person == null)
                return Result.Fail<object>("There is not exist person.");

            return Result.Ok<object>(new
            {
                person.FirstName,
                person.LastName,
                Email = person.GetEmail()
            });
        }
        public object GetSammary(Guid appointmentId)
        {
            var appointment = appointmentRepository.Get(appointmentId);

            if (appointment == null)
            {
                return Result.Fail<Guid>("There is not exist appointment.");
            }


            var baseAddress = CrmExtention.GetLinkBaseAddress();



            return new
            {
                appointment.Id,
                appointment.Name,
                appointment.StartDateTime,
                appointment.Color,
                AppointmentColorType = appointment.AppointmentColorType != null ? new Lookup { Id = appointment.AppointmentColorType.Id, Value = appointment.AppointmentColorType.Name } : null,
                appointment.Duration,
                appointment.ReminderDateSpan,
                appointment.SacondReminderDateSpan,
                appointment.NotifyByEmail,
                appointment.NotifyBySMS,
                appointment.Description,
                appointment.CustomQuestions,
                appointment.CalendarId,
                appointment.Location,
                appointment.AppointmentConfigId,
                appointment.State,
                MeetingLink = $"{baseAddress}Appointment/{appointment.AppointmentConfigId}",
                workFlow = !string.IsNullOrWhiteSpace(appointment.WorkFlow) ? JsonConvert.DeserializeObject<List<FlatConfirmAppointment>>(appointment.WorkFlow) : new List<FlatConfirmAppointment>(),
                meetingBookedDate = appointment.StartDateTime,
                meetingBookedTime = appointment.StartDateTimeSpan,
                Owner = appointment.Owner != null ? new Lookup { Id = appointment.Owner.Id, Value = appointment.Owner.Name } : new Lookup { Id = appointment.CreatedBy.Id, Value = appointment.CreatedBy.Name },
            };
        }

        public Result<object> UpdateStateByUser(Guid id, AppointmentState state)
        {
            List<FlatEvent> conflictedAppointments = null;

            try
            {
                var appointment = appointmentRepository.Get(id);
                if (appointment == null)
                {
                    return Result.Fail<object>("There is not exist appointment.");
                }
                if (appointment.State == state)
                {
                    return Result.Ok<object>($"The appointment is already {state.ToString()}");

                }
                if (appointment.State == AppointmentState.Reject && (state == AppointmentState.Approve || state == AppointmentState.Pending))
                {
                    conflictedAppointments = GetSpecificAppointments(appointment.OwnerId, appointment.StartDateTime, appointment.Duration, appointment.Id);
                    //if (conflictedAppointments != null && conflictedAppointments.Any())
                    //{
                    //    return Result.Fail<object>("Another appointment has already been booked in this time duration.Please choose another Date/Time.");
                    //}
                }
                var historyTracking = new FlatHistoryTrackingValue()
                {
                    OldValue = appointment.State.ToString(),
                    NewValue = state.ToString(),
                    Title = nameof(appointment.State),
                    PropertyType = appointment.State.GetType().Name
                };
                appointment.State = state;
                try
                {
                    if (appointment.NotifyByEmail || appointment.NotifyBySMS)
                    {
                        var appontmentList = totalActivityRepository.Value.GetAll().Where(c => c.ActivityId == appointment.Id).ToList();

                        TenantDetailsModel tenantInfo = null;
                        tenantInfo = TenantHttpClient.Value.GetTenantDetail(appointment.Tenant);
                        DateTime dateTime = DateTime.ParseExact(appointment.StartDateTimeSpan,
                                      "hh:mm tt", CultureInfo.InvariantCulture);
                        TimeSpan span = dateTime.TimeOfDay;
                        bool notSendSMSByNotity = false;
                        //new List<ReferenceDefine>
                        logger.InfoFormat("Befor if for send email to attendess.");
                        //if(appointment.NotifyByEmail)
                        var flat = new FlatAppointment()
                        {
                            AppointmentType = AppointmentType.Web,// appointment.AppointmentType,
                            Id = appointment.Id,
                            SacondReminderDateSpan = appointment.SacondReminderDateSpan,
                            ReminderDateSpan = appointment.ReminderDateSpan,
                            NotifyByEmail = appointment.NotifyByEmail,
                            NotifyBySMS = appointment.NotifyBySMS,
                            NeedConfirmation = appointment.AppointmentConfig != null && appointment.AppointmentConfig.ApproveRequired,
                            StartDate = appointment.StartDateTime,
                            StartDateTime = appointment.StartDateTime,
                            StartDateTimeSpan = appointment.StartDateTimeSpan,
                            State = appointment.State,
                            Name = appointment.Name,
                            Description = appointment.Description,
                            Duration = appointment.Duration,
                            Location = appointment.Location,
                            CalendarId = appointment.CalendarId,
                            Owner = new Lookup
                            {
                                Id = appointment.OwnerId,
                                Value = appointment.Owner.Name
                            },
                            Attendees = appontmentList.Select(s => new ReferenceDefine
                            {
                                Id = s.Id,
                                ReferenceId = s.BusinessTypeObjectId,
                                BusinessObjectType = s.BusinessObjectType,
                                TypeName = (int)s.BusinessObjectType
                            }).ToList()

                        };
                        var variablesList = new Dictionary<string, string>();
                        variablesList.Add("Owner_Name_Value", appointment.Owner.Name);
                        variablesList.Add("Owner_Email_Value", appointment.Owner.Email);
                        variablesList.Add("Owner_Phone_Value", appointment.Owner.Phone);
                        variablesList.Add("{footerText}", string.Empty);//todo to write footer
                        try
                        {
                            var signatures = _SignatureRepository.Value.GetAllSignaturesByUserId(appointment.Owner.Id);
                            var sign = signatures.FirstOrDefault()?.Sign;
                            var splited = sign.Split(new string[] { "body" }, StringSplitOptions.RemoveEmptyEntries);
                            var signiture = (splited[1].Substring(0, splited[1].Length - 2)).Substring(1); ;
                            variablesList.Add("{signature}", signiture);
                        }
                        catch (Exception e)
                        {
                            variablesList.Add("{signature}", string.Empty);
                        }

                        switch (appointment.State)
                        {
                            case AppointmentState.Approve:
                                variablesList.Add("{currentState}", "accepted");
                                break;
                            case AppointmentState.Reject:
                                variablesList.Add("{currentState}", "declined");
                                break;
                            case AppointmentState.Cancel:
                                variablesList.Add("{currentState}", "canceled");
                                break;
                                //currentState
                        }
                        var currentUser = UtilityService.Value.GetCurrentUser().Value;
                        variablesList.Add("User_Name_Value", currentUser.Name);
                        variablesList.Add("User_Email_Value", currentUser.Email);
                        variablesList.Add("User_Phone_Value", currentUser.Phone);
                        variablesList.Add("{titleEmail}", string.Empty);
                        variablesList.Add("{titleName}", string.Empty);
                        if (currentUser.Id != appointment.OwnerId)
                        {
                            variablesList.Add("{byUserValue}", "by " + currentUser.Name);
                        }
                        else
                        {
                            variablesList.Add("{byUserValue}", string.Empty);
                        }

                        variablesList.Add("ReminderDateSpan", string.Empty);
                        variablesList.Add("SecondReminderDateSpan", string.Empty);

                        var emails = UpdateStateByUserNotification(flat, appointment, currentUser, span, ref notSendSMSByNotity, ref tenantInfo, appointment.Tenant, objectVariableList: variablesList);

                        logger.InfoFormat("Befor sync email.");
                        if (appointment.State == AppointmentState.Approve)
                        {
                            SyncCalendar(flat, emails, GoogleCrud.Insert, appointment.Owner);

                            var jobList = new Dictionary<string, string>();
                            jobList = AddReminder(flat, appointment, currentUser, jobList, span, tenantInfo, appointment.Tenant, variablesList);
                            appointment.JobId = JsonConvert.SerializeObject(jobList);

                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(flat.CalendarId))
                            {
                                SyncCalendar(flat, emails, GoogleCrud.Delete, appointment.Owner);
                            }
                            if (!string.IsNullOrEmpty(appointment.JobId))
                            {
                                RemovePreviousReminders(appointment.JobId);
                                appointment.JobId = null;
                            }
                        }
                        appointment.CalendarId = flat.CalendarId;
                        //if (appointment.State != AppointmentState.Approve && !string.IsNullOrEmpty(appointment.JobId))
                        //{
                        //    RemovePreviousReminders(appointment.JobId);
                        //    appointment.JobId = null;
                        //}
                        //else if (appointment.State == AppointmentState.Approve)
                        //{

                        //}
                        appointmentRepository.Update(appointment);
                        _CrmHistoryService.Value.SaveViaFlatList(historyTracking, appointment.Id, BusinessObjectType.Appointment, actionType: ActionType.ChangeStatus);

                    }
                }
                catch (Exception ex)
                {
                    logger.InfoException($"Error in Sync With this app id : {appointment.Id}", ex);
                }

            }
            catch (Exception ex)
            {
                logger.InfoException("Error : ", ex);
                return Result.Fail<object>(ex.Message);

            }

            return Result.Ok<object>(
                (conflictedAppointments != null && conflictedAppointments.Any()) ?
                "Another appointment has already been booked in this time duration." :
                id.ToString());
        }

        private IList<string> UpdateStateByUserNotification(FlatAppointment flat, Appointment appointment, UserInfo currentUser, TimeSpan span, ref bool notSendSMSByNotity, ref TenantDetailsModel tenantInfo, Guid? tenantId, Dictionary<string, string> objectVariableList)
        {// copied from SendNofificationAndReminder - need refactor
            IList<string> totalAttendess = new List<string>();
            IList<string> emails = new List<string>();

            var flatAttendees = flat.Attendees.ToList();
            if (objectVariableList != null && objectVariableList.Any() && objectVariableList.ContainsKey("Attendees_Existed"))
            {
                var existAttendees = JsonConvert.DeserializeObject<List<Guid>>(objectVariableList["Attendees_Existed"]);
                flatAttendees = flat.Attendees.Where(d => existAttendees.Contains(d.ReferenceId)).ToList();
            }
            if (flatAttendees != null && flatAttendees.Any())
            {

                //var createdBy1 = customerService.GetById(appointment.AppointmentConfig.CreatedBy.Id);//todo : if change to owner
                var ownerData = customerService.GetById(appointment.OwnerId);//todo : if change to owner
                List<FlatLead> leadList = new List<FlatLead>();
                foreach (var item in flatAttendees)
                {
                    var flatLead = GetEmailAttendees((item.TypeName == 0 ? item.BusinessObjectType : (BusinessObjectType)item.TypeName), item.ReferenceId);
                    leadList.Add(flatLead);
                    //var flatLead = GetEmailAttendees((BusinessObjectType)item.TypeName, item.ReferenceId);
                    if (flatLead != null)
                    {
                        emails.Add(flatLead.Communication.Email);
                        if (flat.NotifyByEmail)
                        {
                            NotifyByEmailToRecipaintForApproveAppointment(flat, currentUser, tenantInfo, tenantId, true, span, flatLead, objectVariableList);
                            NotifyByEmailToRecipaintForApproveAppointment(flat, currentUser, tenantInfo, tenantId, false, span, flatLead, objectVariableList);
                        }
                        if (flat.NotifyBySMS && (flat.State == AppointmentState.Approve || flat.State == AppointmentState.Reject || flat.State == AppointmentState.Cancel))
                        {

                            var phonenumber = flatLead.Communication.FlatPhoneNumbers.FirstOrDefault(c => c.PhoneType == PhoneType.Mobile);

                            if (flatLead.Communication.Phone != null && PhoneNumberHelper.GetE164(flatLead.Communication.Phone) != null)
                            {
                                string smsBodyCustomer = null;
                                if (flat.State == AppointmentState.Approve)
                                    smsBodyCustomer = GenerateBodyExtension.SmsBodyWithoutApproveCustomer(
                                                                               tenantInfo,
                                                                               appointment.Owner,
                                                                               new FlatAppointment
                                                                               {
                                                                                   StartDateTime = appointment.StartDateTime.Date.Add(span),
                                                                                   StartDateTimeSpan = appointment.StartDateTimeSpan,
                                                                                   Attendees = new List<ReferenceDefine> { new ReferenceDefine { ReferenceName = flatLead.Name } }
                                                                               });
                                if (flat.State == AppointmentState.Reject)
                                    smsBodyCustomer = GenerateBodyExtension.SmsBodyRejectedCustomer(
                                                                               tenantInfo,
                                                                               appointment.Owner,
                                                                               new FlatAppointment
                                                                               {
                                                                                   StartDateTime = appointment.StartDateTime.Date.Add(span),
                                                                                   StartDateTimeSpan = appointment.StartDateTimeSpan,
                                                                                   Attendees = new List<ReferenceDefine> { new ReferenceDefine { ReferenceName = flatLead.Name } }
                                                                               });
                                if (flat.State == AppointmentState.Cancel)
                                    smsBodyCustomer = GenerateBodyExtension.SmsBodyCanceledCustomer(
                                                                               tenantInfo,
                                                                               appointment.Owner,
                                                                               new FlatAppointment
                                                                               {
                                                                                   StartDateTime = appointment.StartDateTime.Date.Add(span),
                                                                                   StartDateTimeSpan = appointment.StartDateTimeSpan,
                                                                                   Attendees = new List<ReferenceDefine> { new ReferenceDefine { ReferenceName = flatLead.Name } }
                                                                               });
                                if (!string.IsNullOrEmpty(smsBodyCustomer)) SmsService.Value.SendSystemSMS(to: flatLead.Communication.Phone,
                                                                 body: smsBodyCustomer, tenant: workingContext.TenantId?.ToString());
                            }


                        }


                    }
                }
                if (appointment.NotifyBySMS && (flat.State == AppointmentState.Approve || flat.State == AppointmentState.Reject || flat.State == AppointmentState.Cancel))
                {
                    if (ownerData != null && ownerData.WorkPhoneNumber != null && PhoneNumberHelper.GetE164(ownerData.WorkPhoneNumber) != null)
                    {
                        string smsBodyOwner = null;
                        if (flat.State == AppointmentState.Approve) smsBodyOwner = GenerateBodyExtension.SmsBodyAcceptedOwner(
                                                   tenantInfo,
                                                   appointment.AppointmentConfig.CreatedBy,
                                                   new FlatAppointment
                                                   {
                                                       StartDateTime = appointment.StartDateTime.Date.Add(span),
                                                       StartDateTimeSpan = appointment.StartDateTimeSpan,
                                                       Attendees = leadList.Select(d => new ReferenceDefine { ReferenceName = d.Name }).ToList(),
                                                       Owner = new Lookup
                                                       {
                                                           Id = appointment.OwnerId,
                                                           Value = appointment.Owner.Name
                                                       },
                                                   });

                        if (flat.State == AppointmentState.Reject) smsBodyOwner = GenerateBodyExtension.SmsBodyRejectedOwner(
                                                   tenantInfo,
                                                   appointment.AppointmentConfig.CreatedBy,
                                                   new FlatAppointment
                                                   {
                                                       StartDateTime = appointment.StartDateTime.Date.Add(span),
                                                       StartDateTimeSpan = appointment.StartDateTimeSpan,
                                                       Attendees = leadList.Select(d => new ReferenceDefine { ReferenceName = d.Name }).ToList(),
                                                       Owner = new Lookup
                                                       {
                                                           Id = appointment.OwnerId,
                                                           Value = appointment.Owner.Name
                                                       },
                                                   });
                        if (flat.State == AppointmentState.Cancel) smsBodyOwner = GenerateBodyExtension.SmsBodyCanceledOwner(
                                                   tenantInfo,
                                                   currentUser,//appointment.AppointmentConfig?.CreatedBy,
                                                   new FlatAppointment
                                                   {
                                                       StartDateTime = appointment.StartDateTime.Date.Add(span),
                                                       StartDateTimeSpan = appointment.StartDateTimeSpan,
                                                       Attendees = leadList.Select(d => new ReferenceDefine { ReferenceName = d.Name }).ToList(),
                                                       Owner = new Lookup
                                                       {
                                                           Id = appointment.OwnerId,
                                                           Value = appointment.Owner.Name
                                                       },
                                                   });

                        SmsService.Value.SendSystemSMS(to: ownerData.WorkPhoneNumber,
                                                         body: smsBodyOwner, tenant: workingContext.TenantId?.ToString());
                    }
                }
            }

            return emails;
        }

        public Result<object> cancelAppointment(Guid id, string cancelToken, string tenant)
        {
            var TenantAccessor = CastleWinsorInstance.Resolve<ITenantAccessor>();
            TenantAccessor.Set(tenant);

            var appointment = appointmentRepository.Get(id);

            if (appointment == null)
            {
                return Result.Fail<object>("There is not exist appointment.");
            }

            if (appointment.CancelToken != cancelToken)
            {
                return Result.Fail<object>("Your token is wrong.");
            }


            appointment.State = AppointmentState.Cancel;
            appointmentRepository.Update(appointment);


            return Result.Ok<object>();

        }

        private NotificationManager GetConfigNotification(NotificationTrigger notificationTrigger)
        {
            var notification = notificationManagerRepository.Value.GetAll()
                   .SingleOrDefault(c => c.NotificationTrigger == notificationTrigger);

            return notification;

        }
        //private void SendNotification(NotificationManager notification, List<Guid> NotificationListeners, string NotificationTypeValue, object NotificationData, Guid? TenantId)
        private void SendNotification(Appointment appointment, UserInfo currentUser)
        {
            try
            {
                if (notificationManager != null && appointment != null && appointment.OwnerId != null)
                {
                    var sendNotification = notificationManager.Value % (int)NotificationDestination.InApp == 0;
                    if (sendNotification)
                    {
                        string notificationMsg = string.Empty;
                        if (((currentUser == null) || currentUser.Id != appointment.OwnerId))
                        {
                            if (notificationManager.NotificationTrigger == NotificationTrigger.AppointmentCreated)
                            {
                                notificationMsg = $"A new appointment scheduled by {currentUser?.Name} on {appointment.StartDateTime.ToLongDateString()}.";
                            }
                            else if (notificationManager.NotificationTrigger == NotificationTrigger.AppointmentUpdated)
                            {
                                notificationMsg = $"Your appointment has been updated by {currentUser?.Name}.";
                            }
                        }
                        else if (appointment.AppointmentType == AppointmentType.Web && notificationManager.NotificationTrigger == NotificationTrigger.AppointmentCreated)
                        {
                            notificationMsg = $"A new appointment has benn scheduled by client on {appointment.StartDateTime.ToLongDateString()}.";
                        }
                        else if ((currentUser != null) && currentUser.Id == appointment.OwnerId)
                        {
                            notificationMsg = $"You scheduled a new appointment on {appointment.StartDateTime.ToLongDateString()}.";

                        }
                        notificationService.Value.PublishNotification(
                            new SignalDetail()
                            {
                                Listeners = new List<Guid> { appointment.OwnerId },
                                PushNotificationType = (int)PushNotificationType.User,
                                NotificationType = (int)BaseObject.BaseBusinessObjects.Enums.NotificationType.Warning,

                                Type = "Appointment",
                                SignalRMessageType = (int)SignalRMessageType.Notify,
                                Data = new
                                {
                                    Message = notificationMsg,
                                    Date = DateTime.UtcNow,
                                    appointment.Id,
                                    appointment.Name

                                },
                                Method = NotificationMethod.showNotification.ToString(),
                                TenantId = appointment.Tenant
                            }
                            );
                    }

                }
            }
            catch (Exception ex)
            {
                logger.InfoException($"Error in SendNotification With this app id : {appointment.Id}", ex);

                // throw;
            }


        }
    }
}
