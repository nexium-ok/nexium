import Head from "next/head";
import SharedAssetPage from "../../../components/sharedAssetPage";
import { getBaseUrl } from "../../../lib/request";
import { getItemUrl, getProductInfoLegacy } from "../../../services/catalog";
import { multiGetAssetThumbnails } from "../../../services/thumbnails";

const ItemPage = props => {
  return <>
    {props.ogTitle && <Head>
      <title>{props.ogTitle} - Nexrev</title>
      <meta property="og:site_name" content="Nexrev" />
      <meta property="og:type" content="website" />
      <meta property="og:title" content={props.ogTitle} />
      <meta property="og:description" content={props.ogDescription || ''} />
      {props.ogImage && <meta property="og:image" content={props.ogImage} />}
      <meta property="og:url" content={props.ogUrl} />
    </Head>}
    <SharedAssetPage idParamName='assetId' nameParamName='name' />
  </>
}

export async function getServerSideProps({ query }) {
  const assetId = query['assetId'];
  const name = query['name'];
  let ogTitle = null;
  let ogDescription = null;
  let ogImage = null;

  try {
    const info = await getProductInfoLegacy(assetId);
    ogTitle = info.Name;
    ogDescription = info.Description;
    try {
      const thumbs = await multiGetAssetThumbnails({ assetIds: [assetId] });
      if (thumbs && thumbs[0] && thumbs[0].imageUrl) {
        ogImage = thumbs[0].imageUrl;
      }
    } catch (e) {
      // thumbnail failure shouldn't block the rest of the embed
    }
  } catch (e) {
    // not a valid/sellable asset (e.g. it's actually a place) - page still
    // renders normally client-side, just without an embed
  }

  return {
    props: {
      ogTitle,
      ogDescription,
      ogImage,
      ogUrl: getBaseUrl().replace(/\/$/, '') + getItemUrl({ assetId, name }),
    },
  };
}

export default ItemPage;