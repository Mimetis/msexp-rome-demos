using AppUwp.Common;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace AppUwp.ViewModels
{
    public class UserViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        private string id;
        private string email;
        private string workEmail;
        private string name;
        private string userPrincipalName;
        private string firstName;
        private string department;
        private string jobTitle;
        private string path;
    
        private bool isLoadedPhoto = false;
        private ImageSource photo = ImageHelper.UnknownPersonImage;
        private Uri photoUri = ImageHelper.UnknownPersonImageUri;
        private static object locker = new object();
        public ImageSource Photo
        {
            get
            {
                if (!IsLoadedPhoto)
                    lock (locker)
                    {
                        if (!IsLoadedPhoto)
                            this.UpdatePhotoAsync(CancellationToken.None);

                    }

                return photo;
            }
            set
            {

                this.photo = value;

                RaisePropertyChanged(nameof(Photo));
            }
        }

        public String Id
        {
            get
            {
                return id;
            }
            set
            {
                this.id = value;

                RaisePropertyChanged(nameof(Id));
            }
        }

        private GraphServiceClient graphService;

        public string Department
        {
            get
            {
                return department;
            }

            set
            {
                department = value;
                RaisePropertyChanged(nameof(Department));
            }
        }
        public string JobTitle
        {
            get
            {
                return jobTitle;
            }

            set
            {
                jobTitle = value;
                RaisePropertyChanged(nameof(JobTitle));
            }
        }
        public string Path
        {
            get
            {
                return path;
            }

            set
            {
                path = value;
                RaisePropertyChanged(nameof(Path));
            }
        }

        public string FirstName
        {
            get
            {
                return this.firstName;
            }
            set
            {
                this.firstName = value;

                RaisePropertyChanged(nameof(FirstName));

            }
        }
        public String Name
        {
            get
            {
                return name;
            }
            set
            {
                this.name = value;


                RaisePropertyChanged(nameof(Name));
            }
        }
        public string Email
        {
            get
            {
                return email;
            }
            set
            {
                this.email = value;

                RaisePropertyChanged(nameof(Email));
            }
        }
        public string WorkEmail
        {
            get
            {
                return workEmail;
            }
            set
            {
                this.workEmail = value;

                RaisePropertyChanged(nameof(WorkEmail));
            }
        }
        public string UserPrincipalName
        {
            get
            {
                return userPrincipalName;
            }

            set
            {
                userPrincipalName = value.Trim();
                RaisePropertyChanged(nameof(UserPrincipalName));
            }
        }


        public bool IsLoadedPhoto
        {
            get
            {
                return isLoadedPhoto;
            }
            private set
            {
                isLoadedPhoto = value;

                RaisePropertyChanged(nameof(IsLoadedPhoto));
            }
        }

        public UserViewModel(User u, GraphServiceClient graphService)
        {
            this.graphService = graphService;
            this.Department = u.Department;
            this.Email = u.Mail;
            this.FirstName = u.GivenName;
            this.Id = u.Id;
            this.JobTitle = u.JobTitle;
            this.Name = u.DisplayName;
            this.UserPrincipalName = u.UserPrincipalName;

        }





        public async Task UpdatePhotoAsync(CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;

            if (IsLoadedPhoto)
                return;

            if (String.IsNullOrEmpty(this.UserPrincipalName))
                return;

            var fileName = $"{this.UserPrincipalName}.png";

            var tuple = await ImageHelper.GetImageFromCache(fileName);

            if (tuple != null)
            {
                this.Photo = tuple.Item1;
            }
            else
            {

                using (Stream photo = await this.graphService.Users[id].Photo.Content.Request().GetAsync())
                {
                    if (photo != null)
                    {
                        // Get byte[] for display.
                        using (BinaryReader reader = new BinaryReader(photo))
                        {
                            byte[] data = reader.ReadBytes((int)photo.Length);

                            var bitmapImage = await ImageHelper.SaveImageToCacheAndGetImage(data, fileName);
                            this.Photo = bitmapImage;
                        }

                    }

                }
            }

            IsLoadedPhoto = true;
        }


    }

}
