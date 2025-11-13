namespace  UCF.Media.Service
{
    using Core.Bridge;

    public sealed class MediaUser : ilUser
    {
        public MediaUser(int useridx) : base(useridx)
        {
            this.useridx = useridx;
        }
    }
}