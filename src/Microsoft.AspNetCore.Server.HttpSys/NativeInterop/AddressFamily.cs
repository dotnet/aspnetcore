// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.HttpSys
{
    /// <devdoc>
    ///    <para>
    ///       Specifies the address families.
    ///    </para>
    /// </devdoc>
    internal enum AddressFamily
    {
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Unknown = -1,   // Unknown

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Unspecified = 0,    // unspecified

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Unix = 1,    // local to host (pipes, portals)

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        InterNetwork = 2,    // internetwork: UDP, TCP, etc.

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ImpLink = 3,    // arpanet imp addresses

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Pup = 4,    // pup protocols: e.g. BSP

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Chaos = 5,    // mit CHAOS protocols

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        NS = 6,    // XEROX NS protocols

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Ipx = NS,   // IPX and SPX

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Iso = 7,    // ISO protocols

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Osi = Iso,  // OSI is ISO

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Ecma = 8,    // european computer manufacturers

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DataKit = 9,    // datakit protocols

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Ccitt = 10,   // CCITT protocols, X.25 etc

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Sna = 11,   // IBM SNA

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DecNet = 12,   // DECnet

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DataLink = 13,   // Direct data link interface

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Lat = 14,   // LAT

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        HyperChannel = 15,   // NSC Hyperchannel

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        AppleTalk = 16,   // AppleTalk

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        NetBios = 17,   // NetBios-style addresses

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        VoiceView = 18,   // VoiceView

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        FireFox = 19,   // FireFox

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Banyan = 21,   // Banyan

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Atm = 22,   // Native ATM Services

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        InterNetworkV6 = 23,   // Internetwork Version 6

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Cluster = 24,   // Microsoft Wolfpack

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Ieee12844 = 25,   // IEEE 1284.4 WG AF

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Irda = 26,   // IrDA

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        NetworkDesigners = 28,   // Network Designers OSI & gateway enabled protocols

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Max = 29,   // Max
    }; // enum AddressFamily
}