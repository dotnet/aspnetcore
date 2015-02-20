using System.Collections.Generic;
using System.Linq;

namespace MvcMusicStore.Models
{
    public class SampleData
    {
        public void Seed(MusicStoreEntities context)
        {
            const string imgUrl = "/images/placeholder.png";

            AddAlbums(context, imgUrl, AddGenres(context), AddArtists(context));

            context.SaveChanges();
        }

        private static void AddAlbums(
            MusicStoreEntities context, 
            string imgUrl, 
            List<Genre> genres,
            List<Artist> artists)
        {
            var albums = new[] 
            {
                new Album
                {
                    Title = "The Best Of The Men At Work",
                    Genre = genres.Single(g => g.Name == "Pop"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Men At Work"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "...And Justice For All",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Metallica"),
                    AlbumArtUrl = "https://ia601005.us.archive.org/12/items/mbid-fce9462d-8444-334d-84d4-2bbf1edfe9b5/mbid-fce9462d-8444-334d-84d4-2bbf1edfe9b5-5114338029_thumb250.jpg"
                },
                new Album
                {
                    Title = "עד גבול האור",
                    Genre = genres.Single(g => g.Name == "World"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "אריק אינשטיין"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Black Light Syndrome",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Terry Bozzio, Tony Levin & Steve Stevens"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "10,000 Days",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Tool"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "11i",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Supreme Beings of Leisure"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "1960",
                    Genre = genres.Single(g => g.Name == "Indie"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Soul-Junk"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "4x4=12 ",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "deadmau5"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "A Copland Celebration, Vol. I",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "London Symphony Orchestra"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "A Lively Mind",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Paul Oakenfold"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "A Matter of Life and Death",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Iron Maiden"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "A Real Dead One",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Iron Maiden"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "A Real Live One",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Iron Maiden"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "A Rush of Blood to the Head",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Coldplay"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "A Soprano Inspired",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Britten Sinfonia, Ivor Bolton & Lesley Garrett"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "A Winter Symphony",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Sarah Brightman"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Abbey Road",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Beatles"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Ace Of Spades",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Motörhead"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Achtung Baby",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "U2"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Acústico MTV",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Os Paralamas Do Sucesso"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Adams, John: The Chairman Dances",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Edo de Waart & San Francisco Symphony"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Adrenaline",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Deftones"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Ænima",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Tool"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Afrociberdelia",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Chico Science & Nação Zumbi"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "After the Goldrush",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Neil Young"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Airdrawn Dagger",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Sasha"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Album Title Goes Here",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "deadmau5"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Alcohol Fueled Brewtality Live! [Disc 1]",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Black Label Society"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Alcohol Fueled Brewtality Live! [Disc 2]",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Black Label Society"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Alive 2007",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Daft Punk"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "All I Ask of You",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Sarah Brightman"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Amen (So Be It)",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Paddy Casey"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Animal Vehicle",
                    Genre = genres.Single(g => g.Name == "Pop"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Axis of Awesome"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Ao Vivo [IMPORT]",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Zeca Pagodinho"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Apocalyptic Love",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Slash"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Appetite for Destruction",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Guns N' Roses"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Are You Experienced?",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Jimi Hendrix"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Arquivo II",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Os Paralamas Do Sucesso"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Arquivo Os Paralamas Do Sucesso",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Os Paralamas Do Sucesso"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "A-Sides",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Soundgarden"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Audioslave",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Audioslave"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Automatic for the People",
                    Genre = genres.Single(g => g.Name == "Alternative"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "R.E.M."),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Axé Bahia 2001",
                    Genre = genres.Single(g => g.Name == "Pop"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Various Artists"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Babel",
                    Genre = genres.Single(g => g.Name == "Alternative"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Mumford & Sons"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Bach: Goldberg Variations",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Wilhelm Kempff"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Bach: The Brandenburg Concertos",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Orchestra of The Age of Enlightenment"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Bach: The Cello Suites",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Yo-Yo Ma"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Bach: Toccata & Fugue in D Minor",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Ton Koopman"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Bad Motorfinger",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Soundgarden"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Balls to the Wall",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Accept"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Banadeek Ta'ala",
                    Genre = genres.Single(g => g.Name == "World"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Amr Diab"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Barbie Girl",
                    Genre = genres.Single(g => g.Name == "Pop"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Aqua"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Bark at the Moon (Remastered)",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Ozzy Osbourne"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Bartok: Violin & Viola Concertos",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Yehudi Menuhin"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Barulhinho Bom",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Marisa Monte"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "BBC Sessions [Disc 1] [Live]",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Led Zeppelin"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "BBC Sessions [Disc 2] [Live]",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Led Zeppelin"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Be Here Now",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Oasis"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Bedrock 11 Compiled & Mixed",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "John Digweed"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Berlioz: Symphonie Fantastique",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Michael Tilson Thomas"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Beyond Good And Evil",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Cult"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Big Bad Wolf ",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Armand Van Helden"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Big Ones",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Aerosmith"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Black Album",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Metallica"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Black Sabbath Vol. 4 (Remaster)",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Black Sabbath"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Black Sabbath",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Black Sabbath"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Black",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Metallica"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Blackwater Park",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Opeth"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Blizzard of Ozz",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Ozzy Osbourne"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Blood",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "In This Moment"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Blue Moods",
                    Genre = genres.Single(g => g.Name == "Jazz"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Incognito"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Blue",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Weezer"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Bongo Fury",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Frank Zappa & Captain Beefheart"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Boys & Girls",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Alabama Shakes"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Brave New World",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Iron Maiden"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "B-Sides 1980-1990",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "U2"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Bunkka",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Paul Oakenfold"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "By The Way",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Red Hot Chili Peppers"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Cake: B-Sides and Rarities",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Cake"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Californication",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Red Hot Chili Peppers"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Carmina Burana",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Boston Symphony Orchestra & Seiji Ozawa"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Carried to Dust (Bonus Track Version)",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Calexico"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Carry On",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Chris Cornell"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Cássia Eller - Sem Limite [Disc 1]",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Cássia Eller"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Chemical Wedding",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Bruce Dickinson"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Chill: Brazil (Disc 1)",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Marcos Valle"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Chill: Brazil (Disc 2)",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Antônio Carlos Jobim"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Chocolate Starfish And The Hot Dog Flavored Water",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Limp Bizkit"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Chronicle, Vol. 1",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Creedence Clearwater Revival"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Chronicle, Vol. 2",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Creedence Clearwater Revival"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Ciao, Baby",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "TheStart"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Cidade Negra - Hits",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Cidade Negra"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Classic Munkle: Turbo Edition",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Munkle"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Classics: The Best of Sarah Brightman",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Sarah Brightman"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Coda",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Led Zeppelin"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Come Away With Me",
                    Genre = genres.Single(g => g.Name == "Jazz"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Norah Jones"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Come Taste The Band",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Deep Purple"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Comfort Eagle",
                    Genre = genres.Single(g => g.Name == "Alternative"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Cake"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Common Reaction",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Uh Huh Her "),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Compositores",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "O Terço"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Contraband",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Velvet Revolver"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Core",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Stone Temple Pilots"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Cornerstone",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Styx"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Cosmicolor",
                    Genre = genres.Single(g => g.Name == "Rap"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "M-Flo"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Cross",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Justice"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Culture of Fear",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Thievery Corporation"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Da Lama Ao Caos",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Chico Science & Nação Zumbi"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Dakshina",
                    Genre = genres.Single(g => g.Name == "World"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Deva Premal"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Dark Side of the Moon",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Pink Floyd"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Death Magnetic",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Metallica"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Deep End of Down",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Above the Fold"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Deep Purple In Rock",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Deep Purple"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Deixa Entrar",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Falamansa"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Deja Vu",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Crosby, Stills, Nash, and Young"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Di Korpu Ku Alma",
                    Genre = genres.Single(g => g.Name == "World"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Lura"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Diary of a Madman (Remastered)",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Ozzy Osbourne"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Diary of a Madman",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Ozzy Osbourne"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Dirt",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Alice in Chains"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Diver Down",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Van Halen"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Djavan Ao Vivo - Vol. 02",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Djavan"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Djavan Ao Vivo - Vol. 1",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Djavan"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Drum'n'bass for Papa",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Plug"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Duluth",
                    Genre = genres.Single(g => g.Name == "Country"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Trampled By Turtles"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Dummy",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Portishead"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Duos II",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Luciana Souza/Romero Lubambo"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Earl Scruggs and Friends",
                    Genre = genres.Single(g => g.Name == "Country"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Earl Scruggs"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Eden",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Sarah Brightman"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "El Camino",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Black Keys"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Elegant Gypsy",
                    Genre = genres.Single(g => g.Name == "Jazz"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Al di Meola"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Elements Of Life",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Tiësto"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Elis Regina-Minha História",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Elis Regina"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Emergency On Planet Earth",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Jamiroquai"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Emotion",
                    Genre = genres.Single(g => g.Name == "World"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Papa Wemba"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "English Renaissance",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The King's Singers"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Every Kind of Light",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Posies"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Faceless",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Godsmack"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Facelift",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Alice in Chains"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Fair Warning",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Van Halen"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Fear of a Black Planet",
                    Genre = genres.Single(g => g.Name == "Rap"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Public Enemy"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Fear Of The Dark",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Iron Maiden"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Feels Like Home",
                    Genre = genres.Single(g => g.Name == "Jazz"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Norah Jones"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Fireball",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Deep Purple"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Fly",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Sarah Brightman"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "For Those About To Rock We Salute You",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "AC/DC"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Four",
                    Genre = genres.Single(g => g.Name == "Blues"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Blues Traveler"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Frank",
                    Genre = genres.Single(g => g.Name == "Pop"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Amy Winehouse"),
                    AlbumArtUrl = "http://coverartarchive.org/release/f51a1d11-98aa-4957-9ad1-f1877aee07a8/3487013199-250.jpg"
                },
                new Album
                {
                    Title = "Further Down the Spiral",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Nine Inch Nails"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Garage Inc. (Disc 1)",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Metallica"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Garage Inc. (Disc 2)",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Metallica"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Garbage",
                    Genre = genres.Single(g => g.Name == "Alternative"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Garbage"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Good News For People Who Love Bad News",
                    Genre = genres.Single(g => g.Name == "Indie"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Modest Mouse"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Gordon",
                    Genre = genres.Single(g => g.Name == "Alternative"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Barenaked Ladies"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Górecki: Symphony No. 3",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Adrian Leaper & Doreen de Feis"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Greatest Hits I",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Queen"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Greatest Hits II",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Queen"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Greatest Hits",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Duck Sauce"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Greatest Hits",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Lenny Kravitz"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Greatest Hits",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Lenny Kravitz"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Greatest Kiss",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Kiss"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Greetings from Michigan",
                    Genre = genres.Single(g => g.Name == "Indie"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Sufjan Stevens"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Group Therapy",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Above & Beyond"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Handel: The Messiah (Highlights)",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Scholars Baroque Ensemble"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Haydn: Symphonies 99 - 104",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Royal Philharmonic Orchestra"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Heart of the Night",
                    Genre = genres.Single(g => g.Name == "Jazz"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Spyro Gyra"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Heart On",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Eagles of Death Metal"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Holy Diver",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Dio"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Homework",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Daft Punk"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Hot Rocks, 1964-1971 (Disc 1)",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Rolling Stones"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Houses Of The Holy",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Led Zeppelin"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "How To Dismantle An Atomic Bomb",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "U2"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Human",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Projected"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Hunky Dory",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "David Bowie"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Hymns",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Projected"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Hysteria",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Def Leppard"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "In Absentia",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Porcupine Tree"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "In Between",
                    Genre = genres.Single(g => g.Name == "Pop"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Paul Van Dyk"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "In Rainbows",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Radiohead"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "In Step",
                    Genre = genres.Single(g => g.Name == "Blues"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Stevie Ray Vaughan & Double Trouble"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "In the court of the Crimson King",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "King Crimson"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "In Through The Out Door",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Led Zeppelin"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "In Your Honor [Disc 1]",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Foo Fighters"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "In Your Honor [Disc 2]",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Foo Fighters"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Indestructible",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Rancid"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Infinity",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Journey"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Into The Light",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "David Coverdale"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Introspective > You",
                    Genre = genres.Single(g => g.Name == "Pop"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Pet Shop Boys"),
                    AlbumArtUrl = "http://coverartarchive.org/release/b3c637f8-ffce-3ed8-a0cf-2b58ecfc1b88/1715773107-250.jpg"
                },
                new Album
                {
                    Title = "Iron Maiden",
                    Genre = genres.Single(g => g.Name == "Blues"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Iron Maiden"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "ISAM",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Amon Tobin"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "IV",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Led Zeppelin"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Jagged Little Pill",
                    Genre = genres.Single(g => g.Name == "Alternative"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Alanis Morissette"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Jagged Little Pill",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Alanis Morissette"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Jorge Ben Jor 25 Anos",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Jorge Ben"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Jota Quest-1995",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Jota Quest"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Kick",
                    Genre = genres.Single(g => g.Name == "Alternative"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "INXS"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Kill 'Em All",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Metallica"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Kind of Blue",
                    Genre = genres.Single(g => g.Name == "Jazz"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Miles Davis"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "King For A Day Fool For A Lifetime",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Faith No More"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Kiss",
                    Genre = genres.Single(g => g.Name == "Pop"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Carly Rae Jepsen"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Last Call",
                    Genre = genres.Single(g => g.Name == "Country"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Cayouche"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Le Freak",
                    Genre = genres.Single(g => g.Name == "R&B"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Chic"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Le Tigre",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Le Tigre"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Led Zeppelin I",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Led Zeppelin"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Led Zeppelin II",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Led Zeppelin"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Led Zeppelin III",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Led Zeppelin"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Let There Be Rock",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "AC/DC"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Little Earthquakes",
                    Genre = genres.Single(g => g.Name == "Alternative"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Tori Amos"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Live [Disc 1]",
                    Genre = genres.Single(g => g.Name == "Blues"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Black Crowes"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Live [Disc 2]",
                    Genre = genres.Single(g => g.Name == "Blues"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Black Crowes"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Live After Death",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Iron Maiden"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Live At Donington 1992 (Disc 1)",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Iron Maiden"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Live At Donington 1992 (Disc 2)",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Iron Maiden"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Live on Earth",
                    Genre = genres.Single(g => g.Name == "Jazz"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Cat Empire"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Live On Two Legs [Live]",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Pearl Jam"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Living After Midnight",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Judas Priest"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Living",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Paddy Casey"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Load",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Metallica"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Love Changes Everything",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Sarah Brightman"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "MacArthur Park Suite",
                    Genre = genres.Single(g => g.Name == "R&B"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Donna Summer"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Machine Head",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Deep Purple"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Magical Mystery Tour",
                    Genre = genres.Single(g => g.Name == "Pop"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Beatles"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Mais Do Mesmo",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Legião Urbana"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Maquinarama",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Skank"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Marasim",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Jagjit Singh"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Mascagni: Cavalleria Rusticana",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "James Levine"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Master of Puppets",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Metallica"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Mechanics & Mathematics",
                    Genre = genres.Single(g => g.Name == "Pop"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Venus Hum"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Mental Jewelry",
                    Genre = genres.Single(g => g.Name == "Alternative"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Live"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Metallics",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Metallica"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "meteora",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Linkin Park"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Meus Momentos",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Gonzaguinha"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Mezmerize",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "System Of A Down"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Mezzanine",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Massive Attack"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Miles Ahead",
                    Genre = genres.Single(g => g.Name == "Jazz"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Miles Davis"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Milton Nascimento Ao Vivo",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Milton Nascimento"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Minas",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Milton Nascimento"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Minha Historia",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Chico Buarque"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Misplaced Childhood",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Marillion"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "MK III The Final Concerts [Disc 1]",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Deep Purple"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Morning Dance",
                    Genre = genres.Single(g => g.Name == "Jazz"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Spyro Gyra"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Motley Crue Greatest Hits",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Mötley Crüe"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Moving Pictures",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Rush"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Mozart: Chamber Music",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Nash Ensemble"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Mozart: Symphonies Nos. 40 & 41",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Berliner Philharmoniker"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Murder Ballads",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Nick Cave and the Bad Seeds"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Music For The Jilted Generation",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Prodigy"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "My Generation - The Very Best Of The Who",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Who"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "My Name is Skrillex",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Skrillex"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Na Pista",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Cláudio Zoli"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Nevermind",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Nirvana"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "New Adventures In Hi-Fi",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "R.E.M."),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "New Divide",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Linkin Park"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "New York Dolls",
                    Genre = genres.Single(g => g.Name == "Punk"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "New York Dolls"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "News Of The World",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Queen"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Nielsen: The Six Symphonies",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Göteborgs Symfoniker & Neeme Järvi"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Night At The Opera",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Queen"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Night Castle",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Trans-Siberian Orchestra"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Nkolo",
                    Genre = genres.Single(g => g.Name == "World"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Lokua Kanza"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "No More Tears (Remastered)",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Ozzy Osbourne"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "No Prayer For The Dying",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Iron Maiden"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "No Security",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Rolling Stones"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "O Brother, Where Art Thou?",
                    Genre = genres.Single(g => g.Name == "Country"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Alison Krauss"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "O Samba Poconé",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Skank"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "O(+>",
                    Genre = genres.Single(g => g.Name == "R&B"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Prince"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Oceania",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Smashing Pumpkins"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Off the Deep End",
                    Genre = genres.Single(g => g.Name == "Pop"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Weird Al"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "OK Computer",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Radiohead"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Olodum",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Olodum"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "One Love",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "David Guetta"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Operation: Mindcrime",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Queensrÿche"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Opiate",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Tool"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Outbreak",
                    Genre = genres.Single(g => g.Name == "Jazz"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Dennis Chambers"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Pachelbel: Canon & Gigue",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "English Concert & Trevor Pinnock"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Paid in Full",
                    Genre = genres.Single(g => g.Name == "Rap"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Eric B. and Rakim"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Para Siempre",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Vicente Fernandez"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Pause",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Four Tet"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Peace Sells... but Who's Buying",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Megadeth"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Physical Graffiti [Disc 1]",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Led Zeppelin"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Physical Graffiti [Disc 2]",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Led Zeppelin"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Physical Graffiti",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Led Zeppelin"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Piece Of Mind",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Iron Maiden"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Pinkerton",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Weezer"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Plays Metallica By Four Cellos",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Apocalyptica"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Pop",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "U2"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Powerslave",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Iron Maiden"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Prenda Minha",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Caetano Veloso"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Presence",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Led Zeppelin"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Pretty Hate Machine",
                    Genre = genres.Single(g => g.Name == "Alternative"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Nine Inch Nails"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Prisoner",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Jezabels"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Privateering",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Mark Knopfler"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Prokofiev: Romeo & Juliet",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Michael Tilson Thomas"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Prokofiev: Symphony No.1",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Sergei Prokofiev & Yuri Temirkanov"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "PSY's Best 6th Part 1",
                    Genre = genres.Single(g => g.Name == "Pop"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "PSY"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Purcell: The Fairy Queen",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "London Classical Players"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Purpendicular",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Deep Purple"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Purple",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Stone Temple Pilots"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Quanta Gente Veio Ver (Live)",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Gilberto Gil"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Quanta Gente Veio ver--Bônus De Carnaval",
                    Genre = genres.Single(g => g.Name == "Jazz"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Gilberto Gil"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Quiet Songs",
                    Genre = genres.Single(g => g.Name == "Jazz"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Aisha Duo"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Raices",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Los Tigres del Norte"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Raising Hell",
                    Genre = genres.Single(g => g.Name == "Rap"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Run DMC"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Raoul and the Kings of Spain ",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Tears For Fears"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Rattle And Hum",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "U2"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Raul Seixas",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Raul Seixas"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Recovery [Explicit]",
                    Genre = genres.Single(g => g.Name == "Rap"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Eminem"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Reign In Blood",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Slayer"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Relayed",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Yes"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "ReLoad",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Metallica"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Respighi:Pines of Rome",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Eugene Ormandy"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Restless and Wild",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Accept"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Retrospective I (1974-1980)",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Rush"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Revelations",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Audioslave"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Revolver",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Beatles"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Ride the Lighting ",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Metallica"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Ride The Lightning",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Metallica"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Ring My Bell",
                    Genre = genres.Single(g => g.Name == "R&B"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Anita Ward"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Riot Act",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Pearl Jam"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Rise of the Phoenix",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Before the Dawn"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Rock In Rio [CD1]",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Iron Maiden"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Rock In Rio [CD2]",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Iron Maiden"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Rock In Rio [CD2]",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Iron Maiden"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Roda De Funk",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Funk Como Le Gusta"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Room for Squares",
                    Genre = genres.Single(g => g.Name == "Pop"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "John Mayer"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Root Down",
                    Genre = genres.Single(g => g.Name == "Jazz"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Jimmy Smith"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Rounds",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Four Tet"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Rubber Factory",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Black Keys"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Rust in Peace",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Megadeth"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Sambas De Enredo 2001",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Various Artists"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Santana - As Years Go By",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Santana"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Santana Live",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Santana"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Saturday Night Fever",
                    Genre = genres.Single(g => g.Name == "R&B"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Bee Gees"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Scary Monsters and Nice Sprites",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Skrillex"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Scheherazade",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Chicago Symphony Orchestra & Fritz Reiner"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "SCRIABIN: Vers la flamme",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Christopher O'Riley"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Second Coming",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Stone Roses"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Serie Sem Limite (Disc 1)",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Tim Maia"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Serie Sem Limite (Disc 2)",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Tim Maia"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Serious About Men",
                    Genre = genres.Single(g => g.Name == "Rap"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Rubberbandits"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Seventh Son of a Seventh Son",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Iron Maiden"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Short Bus",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Filter"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Sibelius: Finlandia",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Berliner Philharmoniker"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Singles Collection",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "David Bowie"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Six Degrees of Inner Turbulence",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Dream Theater"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Slave To The Empire",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "T&N"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Slaves And Masters",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Deep Purple"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Slouching Towards Bethlehem",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Robert James"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Smash",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Offspring"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Something Special",
                    Genre = genres.Single(g => g.Name == "Country"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Dolly Parton"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Somewhere in Time",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Iron Maiden"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Song(s) You Know By Heart",
                    Genre = genres.Single(g => g.Name == "Country"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Jimmy Buffett"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Sound of Music",
                    Genre = genres.Single(g => g.Name == "Punk"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Adicts"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "South American Getaway",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The 12 Cellists of The Berlin Philharmonic"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Sozinho Remix Ao Vivo",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Caetano Veloso"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Speak of the Devil",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Ozzy Osbourne"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Spiritual State",
                    Genre = genres.Single(g => g.Name == "Rap"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Nujabes"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "St. Anger",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Metallica"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Still Life",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Opeth"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Stop Making Sense",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Talking Heads"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Stormbringer",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Deep Purple"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Stranger than Fiction",
                    Genre = genres.Single(g => g.Name == "Punk"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Bad Religion"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Strauss: Waltzes",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Eugene Ormandy"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Supermodified",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Amon Tobin"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Supernatural",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Santana"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Surfing with the Alien (Remastered)",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Joe Satriani"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Switched-On Bach",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Wendy Carlos"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Symphony",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Sarah Brightman"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Szymanowski: Piano Works, Vol. 1",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Martin Roscoe"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Tchaikovsky: The Nutcracker",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "London Symphony Orchestra"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Ted Nugent",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Ted Nugent"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Teflon Don",
                    Genre = genres.Single(g => g.Name == "Rap"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Rick Ross"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Tell Another Joke at the Ol' Choppin' Block",
                    Genre = genres.Single(g => g.Name == "Indie"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Danielson Famile"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Temple of the Dog",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Temple of the Dog"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Ten",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Pearl Jam"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Texas Flood",
                    Genre = genres.Single(g => g.Name == "Blues"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Stevie Ray Vaughan"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Battle Rages On",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Deep Purple"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Beast Live",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Paul D'Ianno"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Best Of 1980-1990",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "U2"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Best of 1990–2000",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Sarah Brightman"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Best of Beethoven",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Nicolaus Esterhazy Sinfonia"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Best Of Billy Cobham",
                    Genre = genres.Single(g => g.Name == "Jazz"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Billy Cobham"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Best of Ed Motta",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Ed Motta"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Best Of Van Halen, Vol. I",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Van Halen"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Bridge",
                    Genre = genres.Single(g => g.Name == "R&B"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Melanie Fiona"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Cage",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Tygers of Pan Tang"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Chicago Transit Authority",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Chicago "),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Chronic",
                    Genre = genres.Single(g => g.Name == "Rap"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Dr. Dre"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Colour And The Shape",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Foo Fighters"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Crane Wife",
                    Genre = genres.Single(g => g.Name == "Alternative"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Decemberists"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Cream Of Clapton",
                    Genre = genres.Single(g => g.Name == "Blues"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Eric Clapton"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Cure",
                    Genre = genres.Single(g => g.Name == "Pop"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Cure"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Dark Side Of The Moon",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Pink Floyd"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Divine Conspiracy",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Epica"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Doors",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Doors"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Dream of the Blue Turtles",
                    Genre = genres.Single(g => g.Name == "Pop"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Sting"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Essential Miles Davis [Disc 1]",
                    Genre = genres.Single(g => g.Name == "Jazz"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Miles Davis"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Essential Miles Davis [Disc 2]",
                    Genre = genres.Single(g => g.Name == "Jazz"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Miles Davis"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Final Concerts (Disc 2)",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Deep Purple"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Final Frontier",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Iron Maiden"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Head and the Heart",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Head and the Heart"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Joshua Tree",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "U2"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Last Night of the Proms",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "BBC Concert Orchestra"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Lumineers",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Lumineers"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Number of The Beast",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Iron Maiden"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Number of The Beast",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Iron Maiden"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Police Greatest Hits",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Police"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Song Remains The Same (Disc 1)",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Led Zeppelin"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Song Remains The Same (Disc 2)",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Led Zeppelin"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Southern Harmony and Musical Companion",
                    Genre = genres.Single(g => g.Name == "Blues"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Black Crowes"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Spade",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Butch Walker & The Black Widows"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Stone Roses",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Stone Roses"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Suburbs",
                    Genre = genres.Single(g => g.Name == "Indie"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Arcade Fire"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Three Tenors Disc1/Disc2",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Carreras, Pavarotti, Domingo"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Trees They Grow So High",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Sarah Brightman"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The Wall",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Pink Floyd"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "The X Factor",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Iron Maiden"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Them Crooked Vultures",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Them Crooked Vultures"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "This Is Happening",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "LCD Soundsystem"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Thunder, Lightning, Strike",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Go! Team"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Time to Say Goodbye",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Sarah Brightman"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Time, Love & Tenderness",
                    Genre = genres.Single(g => g.Name == "Pop"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Michael Bolton"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Tomorrow Starts Today",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Mobile"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Tribute",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Ozzy Osbourne"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Tuesday Night Music Club",
                    Genre = genres.Single(g => g.Name == "Alternative"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Sheryl Crow"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Umoja",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "BLØF"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Under the Pink",
                    Genre = genres.Single(g => g.Name == "Alternative"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Tori Amos"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Undertow",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Tool"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Un-Led-Ed",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Dread Zeppelin"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Unplugged [Live]",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Kiss"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Unplugged",
                    Genre = genres.Single(g => g.Name == "Blues"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Eric Clapton"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Unplugged",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Eric Clapton"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Untrue",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Burial"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Use Your Illusion I",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Guns N' Roses"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Use Your Illusion II",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Guns N' Roses"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Use Your Illusion II",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Guns N' Roses"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Van Halen III",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Van Halen"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Van Halen",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Van Halen"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Version 2.0",
                    Genre = genres.Single(g => g.Name == "Alternative"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Garbage"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Vinicius De Moraes",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Vinícius De Moraes"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Virtual XI",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Iron Maiden"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Voodoo Lounge",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Rolling Stones"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Vozes do MPB",
                    Genre = genres.Single(g => g.Name == "Latin"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Various Artists"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Vs.",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Pearl Jam"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Wagner: Favourite Overtures",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Sir Georg Solti & Wiener Philharmoniker"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Walking Into Clarksdale",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Page & Plant"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Wapi Yo",
                    Genre = genres.Single(g => g.Name == "World"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Lokua Kanza"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "War",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "U2"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Warner 25 Anos",
                    Genre = genres.Single(g => g.Name == "Jazz"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Antônio Carlos Jobim"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Wasteland R&Btheque",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Raunchy"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Watermark",
                    Genre = genres.Single(g => g.Name == "Electronic"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Enya"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "We Were Exploding Anyway",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "65daysofstatic"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Weill: The Seven Deadly Sins",
                    Genre = genres.Single(g => g.Name == "Classical"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Orchestre de l'Opéra de Lyon"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "White Pony",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Deftones"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Who's Next",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Who"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Wish You Were Here",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Pink Floyd"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "With Oden on Our Side",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Amon Amarth"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Worlds",
                    Genre = genres.Single(g => g.Name == "Jazz"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Aaron Goldberg"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Worship Music",
                    Genre = genres.Single(g => g.Name == "Metal"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Anthrax"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "X&Y",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Coldplay"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Xinti",
                    Genre = genres.Single(g => g.Name == "World"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Sara Tavares"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Yano",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Yano"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Yesterday Once More Disc 1/Disc 2",
                    Genre = genres.Single(g => g.Name == "Pop"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "The Carpenters"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Zooropa",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "U2"),
                    AlbumArtUrl = imgUrl
                },
                new Album
                {
                    Title = "Zoso",
                    Genre = genres.Single(g => g.Name == "Rock"),
                    Price = 8.99M,
                    Artist = artists.Single(a => a.Name == "Led Zeppelin"),
                    AlbumArtUrl = imgUrl
                },
            };

            context.Albums.AddRange(albums);
        }

        private static List<Artist> AddArtists(MusicStoreEntities context)
        {
            var artists = new List<Artist>
            {
                new Artist { Name = "65daysofstatic" },
                new Artist { Name = "Aaron Goldberg" },
                new Artist { Name = "Above & Beyond" },
                new Artist { Name = "Above the Fold" },
                new Artist { Name = "AC/DC" },
                new Artist { Name = "Accept" },
                new Artist { Name = "Adicts" },
                new Artist { Name = "Adrian Leaper & Doreen de Feis" },
                new Artist { Name = "Aerosmith" },
                new Artist { Name = "Aisha Duo" },
                new Artist { Name = "Al di Meola" },
                new Artist { Name = "Alabama Shakes" },
                new Artist { Name = "Alanis Morissette" },
                new Artist { Name = "Alberto Turco & Nova Schola Gregoriana" },
                new Artist { Name = "Alice in Chains" },
                new Artist { Name = "Alison Krauss" },
                new Artist { Name = "Amon Amarth" },
                new Artist { Name = "Amon Tobin" },
                new Artist { Name = "Amr Diab" },
                new Artist { Name = "Amy Winehouse" },
                new Artist { Name = "Anita Ward" },
                new Artist { Name = "Anthrax" },
                new Artist { Name = "Antônio Carlos Jobim" },
                new Artist { Name = "Apocalyptica" },
                new Artist { Name = "Aqua" },
                new Artist { Name = "Armand Van Helden" },
                new Artist { Name = "Arcade Fire" },
                new Artist { Name = "Audioslave" },
                new Artist { Name = "Bad Religion" },
                new Artist { Name = "Barenaked Ladies" },
                new Artist { Name = "BBC Concert Orchestra" },
                new Artist { Name = "Bee Gees" },
                new Artist { Name = "Before the Dawn" },
                new Artist { Name = "Berliner Philharmoniker" },
                new Artist { Name = "Billy Cobham" },
                new Artist { Name = "Black Label Society" },
                new Artist { Name = "Black Sabbath" },
                new Artist { Name = "BLØF" },
                new Artist { Name = "Blues Traveler" },
                new Artist { Name = "Boston Symphony Orchestra & Seiji Ozawa" },
                new Artist { Name = "Britten Sinfonia, Ivor Bolton & Lesley Garrett" },
                new Artist { Name = "Bruce Dickinson" },
                new Artist { Name = "Buddy Guy" },
                new Artist { Name = "Burial" },
                new Artist { Name = "Butch Walker & The Black Widows" },
                new Artist { Name = "Caetano Veloso" },
                new Artist { Name = "Cake" },
                new Artist { Name = "Calexico" },
                new Artist { Name = "Carly Rae Jepsen" },
                new Artist { Name = "Carreras, Pavarotti, Domingo" },
                new Artist { Name = "Cássia Eller" },
                new Artist { Name = "Cayouche" },
                new Artist { Name = "Chic" },
                new Artist { Name = "Chicago " },
                new Artist { Name = "Chicago Symphony Orchestra & Fritz Reiner" },
                new Artist { Name = "Chico Buarque" },
                new Artist { Name = "Chico Science & Nação Zumbi" },
                new Artist { Name = "Choir Of Westminster Abbey & Simon Preston" },
                new Artist { Name = "Chris Cornell" },
                new Artist { Name = "Christopher O'Riley" },
                new Artist { Name = "Cidade Negra" },
                new Artist { Name = "Cláudio Zoli" },
                new Artist { Name = "Coldplay" },
                new Artist { Name = "Creedence Clearwater Revival" },
                new Artist { Name = "Crosby, Stills, Nash, and Young" },
                new Artist { Name = "Daft Punk" },
                new Artist { Name = "Danielson Famile" },
                new Artist { Name = "David Bowie" },
                new Artist { Name = "David Coverdale" },
                new Artist { Name = "David Guetta" },
                new Artist { Name = "deadmau5" },
                new Artist { Name = "Deep Purple" },
                new Artist { Name = "Def Leppard" },
                new Artist { Name = "Deftones" },
                new Artist { Name = "Dennis Chambers" },
                new Artist { Name = "Deva Premal" },
                new Artist { Name = "Dio" },
                new Artist { Name = "Djavan" },
                new Artist { Name = "Dolly Parton" },
                new Artist { Name = "Donna Summer" },
                new Artist { Name = "Dr. Dre" },
                new Artist { Name = "Dread Zeppelin" },
                new Artist { Name = "Dream Theater" },
                new Artist { Name = "Duck Sauce" },
                new Artist { Name = "Earl Scruggs" },
                new Artist { Name = "Ed Motta" },
                new Artist { Name = "Edo de Waart & San Francisco Symphony" },
                new Artist { Name = "Elis Regina" },
                new Artist { Name = "Eminem" },
                new Artist { Name = "English Concert & Trevor Pinnock" },
                new Artist { Name = "Enya" },
                new Artist { Name = "Epica" },
                new Artist { Name = "Eric B. and Rakim" },
                new Artist { Name = "Eric Clapton" },
                new Artist { Name = "Eugene Ormandy" },
                new Artist { Name = "Faith No More" },
                new Artist { Name = "Falamansa" },
                new Artist { Name = "Filter" },
                new Artist { Name = "Foo Fighters" },
                new Artist { Name = "Four Tet" },
                new Artist { Name = "Frank Zappa & Captain Beefheart" },
                new Artist { Name = "Fretwork" },
                new Artist { Name = "Funk Como Le Gusta" },
                new Artist { Name = "Garbage" },
                new Artist { Name = "Gerald Moore" },
                new Artist { Name = "Gilberto Gil" },
                new Artist { Name = "Godsmack" },
                new Artist { Name = "Gonzaguinha" },
                new Artist { Name = "Göteborgs Symfoniker & Neeme Järvi" },
                new Artist { Name = "Guns N' Roses" },
                new Artist { Name = "Gustav Mahler" },
                new Artist { Name = "In This Moment" },
                new Artist { Name = "Incognito" },
                new Artist { Name = "INXS" },
                new Artist { Name = "Iron Maiden" },
                new Artist { Name = "Jagjit Singh" },
                new Artist { Name = "James Levine" },
                new Artist { Name = "Jamiroquai" },
                new Artist { Name = "Jimi Hendrix" },
                new Artist { Name = "Jimmy Buffett" },
                new Artist { Name = "Jimmy Smith" },
                new Artist { Name = "Joe Satriani" },
                new Artist { Name = "John Digweed" },
                new Artist { Name = "John Mayer" },
                new Artist { Name = "Jorge Ben" },
                new Artist { Name = "Jota Quest" },
                new Artist { Name = "Journey" },
                new Artist { Name = "Judas Priest" },
                new Artist { Name = "Julian Bream" },
                new Artist { Name = "Justice" },
                new Artist { Name = "Orchestre de l'Opéra de Lyon" },
                new Artist { Name = "King Crimson" },
                new Artist { Name = "Kiss" },
                new Artist { Name = "LCD Soundsystem" },
                new Artist { Name = "Le Tigre" },
                new Artist { Name = "Led Zeppelin" },
                new Artist { Name = "Legião Urbana" },
                new Artist { Name = "Lenny Kravitz" },
                new Artist { Name = "Les Arts Florissants & William Christie" },
                new Artist { Name = "Limp Bizkit" },
                new Artist { Name = "Linkin Park" },
                new Artist { Name = "Live" },
                new Artist { Name = "Lokua Kanza" },
                new Artist { Name = "London Symphony Orchestra" },
                new Artist { Name = "Los Tigres del Norte" },
                new Artist { Name = "Luciana Souza/Romero Lubambo" },
                new Artist { Name = "Lulu Santos" },
                new Artist { Name = "Lura" },
                new Artist { Name = "Marcos Valle" },
                new Artist { Name = "Marillion" },
                new Artist { Name = "Marisa Monte" },
                new Artist { Name = "Mark Knopfler" },
                new Artist { Name = "Martin Roscoe" },
                new Artist { Name = "Massive Attack" },
                new Artist { Name = "Maurizio Pollini" },
                new Artist { Name = "Megadeth" },
                new Artist { Name = "Mela Tenenbaum, Pro Musica Prague & Richard Kapp" },
                new Artist { Name = "Melanie Fiona" },
                new Artist { Name = "Men At Work" },
                new Artist { Name = "Metallica" },
                new Artist { Name = "M-Flo" },
                new Artist { Name = "Michael Bolton" },
                new Artist { Name = "Michael Tilson Thomas" },
                new Artist { Name = "Miles Davis" },
                new Artist { Name = "Milton Nascimento" },
                new Artist { Name = "Mobile" },
                new Artist { Name = "Modest Mouse" },
                new Artist { Name = "Mötley Crüe" },
                new Artist { Name = "Motörhead" },
                new Artist { Name = "Mumford & Sons" },
                new Artist { Name = "Munkle" },
                new Artist { Name = "Nash Ensemble" },
                new Artist { Name = "Neil Young" },
                new Artist { Name = "New York Dolls" },
                new Artist { Name = "Nick Cave and the Bad Seeds" },
                new Artist { Name = "Nicolaus Esterhazy Sinfonia" },
                new Artist { Name = "Nine Inch Nails" },
                new Artist { Name = "Nirvana" },
                new Artist { Name = "Norah Jones" },
                new Artist { Name = "Nujabes" },
                new Artist { Name = "O Terço" },
                new Artist { Name = "Oasis" },
                new Artist { Name = "Olodum" },
                new Artist { Name = "Opeth" },
                new Artist { Name = "Orchestra of The Age of Enlightenment" },
                new Artist { Name = "Os Paralamas Do Sucesso" },
                new Artist { Name = "Ozzy Osbourne" },
                new Artist { Name = "Paddy Casey" },
                new Artist { Name = "Page & Plant" },
                new Artist { Name = "Papa Wemba" },
                new Artist { Name = "Paul D'Ianno" },
                new Artist { Name = "Paul Oakenfold" },
                new Artist { Name = "Paul Van Dyk" },
                new Artist { Name = "Pearl Jam" },
                new Artist { Name = "Pet Shop Boys" },
                new Artist { Name = "Pink Floyd" },
                new Artist { Name = "Plug" },
                new Artist { Name = "Porcupine Tree" },
                new Artist { Name = "Portishead" },
                new Artist { Name = "Prince" },
                new Artist { Name = "Projected" },
                new Artist { Name = "PSY" },
                new Artist { Name = "Public Enemy" },
                new Artist { Name = "Queen" },
                new Artist { Name = "Queensrÿche" },
                new Artist { Name = "R.E.M." },
                new Artist { Name = "Radiohead" },
                new Artist { Name = "Rancid" },
                new Artist { Name = "Raul Seixas" },
                new Artist { Name = "Raunchy" },
                new Artist { Name = "Red Hot Chili Peppers" },
                new Artist { Name = "Rick Ross" },
                new Artist { Name = "Robert James" },
                new Artist { Name = "London Classical Players" },
                new Artist { Name = "Royal Philharmonic Orchestra" },
                new Artist { Name = "Run DMC" },
                new Artist { Name = "Rush" },
                new Artist { Name = "Santana" },
                new Artist { Name = "Sara Tavares" },
                new Artist { Name = "Sarah Brightman" },
                new Artist { Name = "Sasha" },
                new Artist { Name = "Scholars Baroque Ensemble" },
                new Artist { Name = "Scorpions" },
                new Artist { Name = "Sergei Prokofiev & Yuri Temirkanov" },
                new Artist { Name = "Sheryl Crow" },
                new Artist { Name = "Sir Georg Solti & Wiener Philharmoniker" },
                new Artist { Name = "Skank" },
                new Artist { Name = "Skrillex" },
                new Artist { Name = "Slash" },
                new Artist { Name = "Slayer" },
                new Artist { Name = "Soul-Junk" },
                new Artist { Name = "Soundgarden" },
                new Artist { Name = "Spyro Gyra" },
                new Artist { Name = "Stevie Ray Vaughan & Double Trouble" },
                new Artist { Name = "Stevie Ray Vaughan" },
                new Artist { Name = "Sting" },
                new Artist { Name = "Stone Temple Pilots" },
                new Artist { Name = "Styx" },
                new Artist { Name = "Sufjan Stevens" },
                new Artist { Name = "Supreme Beings of Leisure" },
                new Artist { Name = "System Of A Down" },
                new Artist { Name = "T&N" },
                new Artist { Name = "Talking Heads" },
                new Artist { Name = "Tears For Fears" },
                new Artist { Name = "Ted Nugent" },
                new Artist { Name = "Temple of the Dog" },
                new Artist { Name = "Terry Bozzio, Tony Levin & Steve Stevens" },
                new Artist { Name = "The 12 Cellists of The Berlin Philharmonic" },
                new Artist { Name = "The Axis of Awesome" },
                new Artist { Name = "The Beatles" },
                new Artist { Name = "The Black Crowes" },
                new Artist { Name = "The Black Keys" },
                new Artist { Name = "The Carpenters" },
                new Artist { Name = "The Cat Empire" },
                new Artist { Name = "The Cult" },
                new Artist { Name = "The Cure" },
                new Artist { Name = "The Decemberists" },
                new Artist { Name = "The Doors" },
                new Artist { Name = "The Eagles of Death Metal" },
                new Artist { Name = "The Go! Team" },
                new Artist { Name = "The Head and the Heart" },
                new Artist { Name = "The Jezabels" },
                new Artist { Name = "The King's Singers" },
                new Artist { Name = "The Lumineers" },
                new Artist { Name = "The Offspring" },
                new Artist { Name = "The Police" },
                new Artist { Name = "The Posies" },
                new Artist { Name = "The Prodigy" },
                new Artist { Name = "The Rolling Stones" },
                new Artist { Name = "The Rubberbandits" },
                new Artist { Name = "The Smashing Pumpkins" },
                new Artist { Name = "The Stone Roses" },
                new Artist { Name = "The Who" },
                new Artist { Name = "Them Crooked Vultures" },
                new Artist { Name = "TheStart" },
                new Artist { Name = "Thievery Corporation" },
                new Artist { Name = "Tiësto" },
                new Artist { Name = "Tim Maia" },
                new Artist { Name = "Ton Koopman" },
                new Artist { Name = "Tool" },
                new Artist { Name = "Tori Amos" },
                new Artist { Name = "Trampled By Turtles" },
                new Artist { Name = "Trans-Siberian Orchestra" },
                new Artist { Name = "Tygers of Pan Tang" },
                new Artist { Name = "U2" },
                new Artist { Name = "UB40" },
                new Artist { Name = "Uh Huh Her " },
                new Artist { Name = "Van Halen" },
                new Artist { Name = "Various Artists" },
                new Artist { Name = "Velvet Revolver" },
                new Artist { Name = "Venus Hum" },
                new Artist { Name = "Vicente Fernandez" },
                new Artist { Name = "Vinícius De Moraes" },
                new Artist { Name = "Weezer" },
                new Artist { Name = "Weird Al" },
                new Artist { Name = "Wendy Carlos" },
                new Artist { Name = "Wilhelm Kempff" },
                new Artist { Name = "Yano" },
                new Artist { Name = "Yehudi Menuhin" },
                new Artist { Name = "Yes" },
                new Artist { Name = "Yo-Yo Ma" },
                new Artist { Name = "Zeca Pagodinho" },
                new Artist { Name = "אריק אינשטיין" }
            };

            context.Artists.AddRange(artists);

            return artists;
        }

        private static List<Genre> AddGenres(MusicStoreEntities context)
        {
            var genres = new List<Genre>
            {
                new Genre { Name = "Pop" },
                new Genre { Name = "Rock" },
                new Genre { Name = "Jazz" },
                new Genre { Name = "Metal" },
                new Genre { Name = "Electronic" },
                new Genre { Name = "Blues" },
                new Genre { Name = "Latin" },
                new Genre { Name = "Rap" },
                new Genre { Name = "Classical" },
                new Genre { Name = "Alternative" },
                new Genre { Name = "Country" },
                new Genre { Name = "R&B" },
                new Genre { Name = "Indie" },
                new Genre { Name = "Punk" },
                new Genre { Name = "World" }
            };

            context.Genres.AddRange(genres);

            return genres;
        }
    }
}