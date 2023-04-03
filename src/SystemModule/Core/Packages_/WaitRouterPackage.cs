namespace SystemModule.CoreSocket
{
    /// <summary>
    /// 可等待的路由包。
    /// </summary>
    public class WaitRouterPackage : MsgRouterPackage, IWaitResult
    {
        /// <inheritdoc/>
        public long Sign { get; set; }

        /// <inheritdoc/>
        public byte Status { get; set; }

        /// <inheritdoc/>
        public override void PackageBody(ByteBlock byteBlock)
        {
            base.PackageBody(byteBlock);
            byteBlock.Write(Sign);
            byteBlock.Write(Status);
        }

        /// <inheritdoc/>
        public override void UnpackageBody(ByteBlock byteBlock)
        {
            base.UnpackageBody(byteBlock);
            Sign = byteBlock.ReadInt64();
            Status = (byte)byteBlock.ReadByte();
        }
    }
}